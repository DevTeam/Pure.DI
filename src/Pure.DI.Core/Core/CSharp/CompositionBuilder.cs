// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
namespace Pure.DI.Core.CSharp;

internal class CompositionBuilder: CodeGraphWalker<BuildContext>, IBuilder<DependencyGraph, CompositionCode>
{
    private static readonly string InjectionStatement = $"{Variable.InjectionMarker};";
    private readonly ILogger<CompositionBuilder> _logger;
    private readonly IVarIdGenerator _idGenerator;
    private readonly IFilter _filter;
    private readonly Dictionary<Compilation, INamedTypeSymbol?> _disposableTypes = new();
    private readonly Dictionary<Root, ImmutableArray<Line>> _roots = new();

    public CompositionBuilder(
        ILogger<CompositionBuilder> logger,
        IVarIdGenerator idGenerator,
        IFilter filter)
        : base(idGenerator)
    {
        _logger = logger;
        _idGenerator = idGenerator;
        _filter = filter;
    }
    
    private CompositionBuilder CreateChildBuilder() => new(_logger, _idGenerator, _filter);

    public CompositionCode Build(
        DependencyGraph dependencyGraph,
        CancellationToken cancellationToken)
    {
        _roots.Clear();
        var variables = new Dictionary<MdBinding, Variable>();
        var isThreadSafe = dependencyGraph.Source.Hints.GetHint(Hint.ThreadSafe, SettingState.On) == SettingState.On;
        var context = new BuildContext(isThreadSafe, ImmutableDictionary<MdBinding, Variable>.Empty, new LinesBuilder());
        VisitGraph(context, dependencyGraph, variables, cancellationToken);
        var fields = variables.Select(i => i.Value).ToImmutableArray();
        return new CompositionCode(
            dependencyGraph,
            dependencyGraph.Source.Name,
            dependencyGraph.Source.UsingDirectives,
            GetSingletons(fields),
            GetArgs(fields),
            _roots
                .Select(i => i.Key with { Lines = i.Value })
                .OrderByDescending(i => i.IsPublic)
                .ThenBy(i => i.Node.Binding.Id)
                .ThenBy(i => i.PropertyName)
                .ToImmutableArray(),
            variables.Values.Count(IsDisposable),
            new LinesBuilder(),
            0
        );
    }

    protected override void VisitRoot(
        BuildContext context,
        DependencyGraph dependencyGraph,
        IDictionary<MdBinding, Variable> variables,
        Root root,
        CancellationToken cancellationToken)
    {
        if (_roots.ContainsKey(root))
        {
            return;
        }

        var newContext = context with { Variables = variables, Code = new LinesBuilder() };
        base.VisitRoot(newContext, dependencyGraph, variables, root, cancellationToken);
        _roots.Add(root, newContext.Code.Lines.ToImmutableArray());
    }

    protected override void VisitBlock(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Block block,
        CancellationToken cancellationToken)
    {
        var isSingletonInitBlock = block.Root is { IsCreated: false, Node.Lifetime: Lifetime.Singleton };
        if (isSingletonInitBlock)
        {
            StartSingletonInitBlock(context, block.Root);
        }

        base.VisitBlock(context, dependencyGraph, root, block, cancellationToken);

        if (isSingletonInitBlock)
        {
            FinishSingletonInitBlock(context, block.Root);
            if (context.IsRootContext && block.Root == root)
            {
                context.Code.AppendLines(GenerateReturnStatements(context, block.Root));
            }
        }
    }

    protected override void VisitImplementation(
        BuildContext context,
        Variable root,
        Instantiation instantiation,
        in DpImplementation implementation,
        CancellationToken cancellationToken)
    {
        base.VisitImplementation(context, root, instantiation, implementation, cancellationToken);
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
    }

    protected override void VisitConstructor(
        BuildContext context,
        Instantiation instantiation,
        in DpImplementation implementation,
        in DpMethod constructor,
        in ImmutableArray<Variable> constructorArguments,
        in ImmutableArray<(Variable InitOnlyVariable, DpProperty InitOnlyProperty)> initOnlyProperties)
    {
        base.VisitConstructor(context, instantiation, implementation, constructor, constructorArguments, initOnlyProperties);
        
        string InjectVar(Variable variable) => Inject(context, variable);
        var args = string.Join(", ", constructorArguments.Select(InjectVar));
        string newStatement;
        if (!instantiation.Target.InstanceType.IsTupleType)
        {
            newStatement = $"new {instantiation.Target.InstanceType}({args})";
        }
        else
        {
            newStatement = $"({args})";
        }

        context.Code.AppendLines(GenerateDeclareStatements(instantiation.Target, newStatement, initOnlyProperties.Any() ? "" : ";"));
        if (initOnlyProperties.Any())
        {
            context.Code.AppendLine("{");
            using (context.Code.Indent())
            {
                for (var index = 0; index < initOnlyProperties.Length; index++)
                {
                    var property = initOnlyProperties[index];
                    context.Code.AppendLine($"{property.InitOnlyProperty.Property.Name} = {Inject(context, property.InitOnlyVariable)}{(index < initOnlyProperties.Length - 1 ? "," : "")}");
                }
            }

            context.Code.AppendLine("};");
        }
        

        context.Code.AppendLines(GenerateOnInstanceCreatedStatements(context, instantiation.Target));
    }

    protected override void VisitField(
        BuildContext context,
        Instantiation instantiation,
        in DpField field,
        Variable fieldVariable)
    {
        base.VisitField(context, instantiation, field, fieldVariable);
        context.Code.AppendLine($"{instantiation.Target.Name}.{field.Field.Name} = {Inject(context, fieldVariable)};");
    }

    protected override void VisitProperty(
        BuildContext context,
        Instantiation instantiation,
        in DpProperty property,
        Variable propertyVariable)
    {
        base.VisitProperty(context, instantiation, property, propertyVariable);
        context.Code.AppendLine($"{instantiation.Target.Name}.{property.Property.Name} = {Inject(context, propertyVariable)};");
    }

    protected override void VisitMethod(
        BuildContext context,
        Instantiation instantiation,
        in DpMethod method,
        in ImmutableArray<Variable> methodArguments)
    {
        base.VisitMethod(context, instantiation, method, methodArguments);

        string InjectVariable(Variable variable) => Inject(context, variable);
        var args = string.Join(", ", methodArguments.Select(InjectVariable));
        context.Code.AppendLine($"{instantiation.Target.Name}.{method.Method.Name}({args});");
    }

    protected override void VisitArg(
        BuildContext context,
        Variable rootVariable,
        Instantiation instantiation,
        in DpArg dpArg)
    {
        if (context.IsRootContext && instantiation.Target == rootVariable)
        {
            context.Code.AppendLines(GenerateReturnStatements(context, instantiation.Target));
        }

        base.VisitArg(context, rootVariable, instantiation, dpArg);
    }

    protected override void VisitEnumerableConstruct(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation,
        CancellationToken cancellationToken)
    {
        if (instantiation.Target.IsCreated)
        {
            return;
        }

        var localFuncName = $"LocalFunc_{instantiation.Target.Name}";
        context.Code.AppendLine($"{instantiation.Target.InstanceType} {localFuncName}()");
        context.Code.AppendLine("{");
        using (context.Code.Indent())
        {
            var isFirst = true;
            foreach (var arg in instantiation.Arguments)
            {
                if (!isFirst)
                {
                    context.Code.AppendLine();
                }

                arg.IsCreated = false;
                VisitRootVariable(context with { IsRootContext = false }, dependencyGraph, context.Variables, arg, cancellationToken);
                context.Code.AppendLine($"yield return {Inject(context, arg)};");
                isFirst = false;
            }
        }
        context.Code.AppendLine("}");
        context.Code.AppendLine();
        context.Code.AppendLine($"{instantiation.Target.InstanceType} {instantiation.Target.Name} = {localFuncName}();");
        context.Code.AppendLines(GenerateOnInstanceCreatedStatements(context, instantiation.Target));
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
    }

    protected override void VisitArrayConstruct(
        BuildContext context,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation)
    {
        if (instantiation.Target.IsCreated)
        {
            return;
        }

        string InjectVariable(Variable variable) => Inject(context, variable);
        context.Code.AppendLine($"{instantiation.Target.InstanceType} {instantiation.Target.Name} = new {construct.Source.ElementType}[{instantiation.Arguments.Length.ToString()}] {{ {string.Join(", ", instantiation.Arguments.Select(InjectVariable))} }};");
        context.Code.AppendLines(GenerateOnInstanceCreatedStatements(context, instantiation.Target));
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
    }

    protected override void VisitSpanConstruct(
        BuildContext context,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation)
    {
        if (instantiation.Target.IsCreated)
        {
            return;
        }
        
        string InjectVariable(Variable variable) => Inject(context, variable);
        var createArray = $"{construct.Source.ElementType}[{instantiation.Arguments.Length.ToString()}] {{ {string.Join(", ", instantiation.Arguments.Select(InjectVariable))} }}";
        var createInstance = 
            construct.Source.ElementType.IsValueType
            && construct.Binding.SemanticModel.Compilation.GetLanguageVersion() >= LanguageVersion.CSharp7_3
            && !IsJustReturn(context, root, instantiation) 
                ? $"stackalloc {createArray}"
                : $"new {Constant.SystemNamespace}Span<{construct.Source.ElementType}>(new {createArray})";
        context.Code.AppendLine($"{instantiation.Target.InstanceType} {instantiation.Target.Name} = {createInstance};");
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
    }

    protected override void VisitCompositionConstruct(
        BuildContext context,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation)
    {
        if (instantiation.Target.IsCreated)
        {
            return;
        }

        context.Code.AppendLines(GenerateDeclareStatements(instantiation.Target, "this"));
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
    }

    protected override void VisitOnCannotResolve(BuildContext context, Variable root, in DpConstruct construct, Instantiation instantiation)
    {
        if (instantiation.Target.IsCreated)
        {
            return;
        }

        context.Code.AppendLines(GenerateDeclareStatements(instantiation.Target, $"{Constant.OnCannotResolve}<{instantiation.Target.ContractType}>({instantiation.Target.Injection.Tag.ValueToString()}, {instantiation.Target.Node.Lifetime.ValueToString()})"));
        context.Code.AppendLines(GenerateOnInstanceCreatedStatements(context, instantiation.Target));
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
    }

    private void AddReturnStatement(BuildContext context, Variable root, Instantiation instantiation)
    {
        if (IsJustReturn(context, root, instantiation))
        {
            context.Code.AppendLines(GenerateReturnStatements(context, instantiation.Target));
        }
    }

    private static bool IsJustReturn(BuildContext context, Variable root, Instantiation instantiation) => 
        context.IsRootContext && instantiation.Target == root && instantiation.Target.Node.Lifetime != Lifetime.Singleton;

    protected override void VisitFactory(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Instantiation instantiation,
        in DpFactory factory,
        CancellationToken cancellationToken)
    {
        if (instantiation.Target.IsCreated)
        {
            return;
        }
        
        var code = context.Code;
        if (!instantiation.Target.IsDeclared)
        {
            code.AppendLine($"{instantiation.Target.InstanceType} {instantiation.Target.Name};");
        }
        
        var factoryBuildContext = context with
        {
            IsRootContext = false,
            ContextTag = root.Injection.Tag
        };
        
        if (factory.Source.Factory.Block is null)
        {
            code.Append($"{instantiation.Target.Name} = ");
        }

        RewriteFactory(factoryBuildContext, context.IsRootContext, dependencyGraph, instantiation, factory, cancellationToken);
        code.AppendLines(GenerateOnInstanceCreatedStatements(context, instantiation.Target));

        var justReturn = context.IsRootContext && instantiation.Target == root && instantiation.Target.Node.Lifetime != Lifetime.Singleton;
        if (justReturn)
        {
            code.AppendLines(GenerateReturnStatements(context, instantiation.Target));
        }
        
        instantiation.Target.IsCreated = true;
        base.VisitFactory(context, dependencyGraph, root, instantiation, factory, cancellationToken);
    }

    private void RewriteFactory(
        BuildContext context,
        bool isRootContext,
        DependencyGraph dependencyGraph,
        Instantiation instantiation,
        DpFactory factory,
        CancellationToken cancellationToken)
    {
        var code = context.Code;

        // Rewrites syntax tree
        var finishLabel = $"label{_idGenerator.NextId.ToString()}{Variable.Postfix}";
        var injections = new List<FactoryRewriter.Injection>();
        var factoryRewriter = new FactoryRewriter(factory, instantiation.Target, context.ContextTag, finishLabel, injections);
        var lambda = factoryRewriter.Rewrite(factory.Source.Factory);

        SyntaxNode syntaxNode = lambda.Block is not null
            ? lambda.Block
            : SyntaxFactory.ExpressionStatement((ExpressionSyntax)lambda.Body);

        var lines = syntaxNode.ToString().Split('\n');
        
        // Replaces injection markers by injection code
        var factoryCodeBuilder = CreateChildBuilder();
        using var resolvers = injections.Zip(instantiation.Arguments, (injection, argument) => (injection, argument)).GetEnumerator();
        var indent = new Indent(0);
        foreach (var line in lines)
        {
            if (line.Trim() == InjectionStatement && resolvers.MoveNext())
            {
                // When an injection marker
                var resolver = resolvers.Current;
                var injectedVariable = CreateVariable(dependencyGraph, context.Variables, resolver.argument.Node, resolver.argument.Injection);
                using (code.Indent(indent.Value))
                {
                    factoryCodeBuilder.VisitRootVariable(context, dependencyGraph, context.Variables, injectedVariable, cancellationToken);
                    code.AppendLine($"{(resolver.injection.DeclarationRequired ? $"{resolver.argument.Injection.Type} " : "")}{resolver.injection.VariableName} = {Inject(context, injectedVariable)};");
                }
            }
            else
            {
                // When a code
                var len = 0;
                for (; len < line.Length && line[len] == ' '; len++)
                {
                }

                indent = len / Formatting.IndentSize;
                code.AppendLine(line);
            }
        }

        if (factoryRewriter.IsFinishMarkRequired)
        {
            code.AppendLine($"{finishLabel}:");
            if (isRootContext)
            {
                // The NOP statement is required due to the label is last statement in the block
                code.AppendLine(";");
            }
        }
    }

    private static void StartSingletonInitBlock(BuildContext context, Variable variable)
    {
        var checkExpression = variable.InstanceType.IsValueType switch
        {
            true => $"!{variable.Name}Created",
            false => $"{Constant.SystemNamespace}Object.ReferenceEquals({variable.Name}, null)"
        };

        var code = context.Code;
        if (context.IsThreadSafe)
        {
            code.AppendLine($"if ({checkExpression})");
            code.AppendLine("{");
            code.IncIndent();
            code.AppendLine($"lock ({Variable.DisposablesFieldName})");
            code.AppendLine("{");
            code.IncIndent();
        }

        code.AppendLine($"if ({checkExpression})");
        code.AppendLine("{");
        code.IncIndent();
    }

    private void FinishSingletonInitBlock(BuildContext context, Variable variable)
    {
        var code = context.Code;
        if (IsDisposable(variable))
        {
            code.AppendLine($"{Variable.DisposablesFieldName}[{Variable.DisposeIndexFieldName}++] = {variable.Name};");
        }
        
        if (variable.InstanceType.IsValueType)
        {
            code.AppendLine($"{Constant.SystemNamespace}Threading.Thread.MemoryBarrier();");
            code.AppendLine($"{variable.Name}Created = true;");
        }

        code.DecIndent();
        code.AppendLine("}");
        if (context.IsThreadSafe)
        {
            code.DecIndent();
            code.AppendLine("}");
            code.DecIndent();
            code.AppendLine("}");
            code.AppendLine();
        }
    }
    
    private static IEnumerable<Line> GenerateDeclareStatements(Variable variable, string instantiation, string lastChars = ";")
    {
        var declaration = variable.IsDeclared ? $"{variable.Name}" : $"{variable.InstanceType} {variable.Name}";
        yield return new Line(0, $"{declaration} = {instantiation}{lastChars}");
    }
    
    private IEnumerable<Line> GenerateOnInstanceCreatedStatements(BuildContext context, Variable variable)
    {
        if (variable.Node.Arg is not null)
        {
            yield break;
        }

        if (variable.Source.Source.Hints.GetHint(Hint.OnInstanceCreation) != SettingState.On)
        {
            yield break;
        }

        if (!_filter.IsMeetRegularExpression(
                variable.Source.Source,
                (Hint.OnInstanceCreationImplementationTypeNameRegularExpression, variable.Node.Type.ToString()),
                (Hint.OnInstanceCreationTagRegularExpression, variable.Injection.Tag.ValueToString()),
                (Hint.OnInstanceCreationLifetimeRegularExpression, variable.Node.Lifetime.ValueToString())))
        {
            yield break;
        }

        var tag = GetTag(context, variable);
        yield return new Line(0, $"{Constant.OnInstanceCreationMethodName}<{variable.InstanceType}>(ref {variable.Name}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()})" + ";");
    }

    private static object? GetTag(BuildContext context, Variable variable)
    {
        var tag = variable.Injection.Tag;
        if (ReferenceEquals(tag, MdTag.ContextTag))
        {
            tag = context.ContextTag;
        }

        return tag;
    }

    private IEnumerable<Line> GenerateReturnStatements(BuildContext context, Variable variable, string lastChars = ";")
    {
        yield return new Line(0, $"return {Inject(context, variable)}{lastChars}");
    }

    private string Inject(BuildContext context, Variable variable)
    {
        if (variable.Source.Source.Hints.GetHint(Hint.OnDependencyInjection) != SettingState.On)
        {
            return variable.Name;
        }

        if (!_filter.IsMeetRegularExpression(
                variable.Source.Source,
                (Hint.OnDependencyInjectionImplementationTypeNameRegularExpression, variable.Node.Type.ToString()),
                (Hint.OnDependencyInjectionContractTypeNameRegularExpression, variable.Injection.Type.ToString()),
                (Hint.OnDependencyInjectionTagRegularExpression, variable.Injection.Tag.ValueToString()),
                (Hint.OnDependencyInjectionLifetimeRegularExpression, variable.Node.Lifetime.ValueToString())))
        {
            return variable.Name;
        }
        
        var tag = GetTag(context, variable);
        return $"{Constant.OnDependencyInjectionMethodName}<{variable.ContractType}>({variable.Name}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()})";
    }
    
    private bool IsDisposable(Variable variable)
    {
        var compilation = variable.Node.Binding.SemanticModel.Compilation;
        if (!_disposableTypes.TryGetValue(compilation, out var disposableType))
        {
            disposableType = compilation.GetTypeByMetadataName(Constant.IDisposableInterfaceName);
            _disposableTypes.Add(compilation, disposableType);
        }
        
        return disposableType is not null && variable.Node.Type.AllInterfaces.Any(i => disposableType.Equals(i, SymbolEqualityComparer.Default));
    }
    
    private static ImmutableArray<Variable> GetSingletons(in ImmutableArray<Variable> fields) =>
        fields
            .Where(i => Equals(i.Node.Lifetime, Lifetime.Singleton))
            .OrderBy(i => i.Node.Binding.Id)
            .ToImmutableArray();

    private static ImmutableArray<Variable> GetArgs(in ImmutableArray<Variable> fields) =>
        fields
            .Where(i => i.Node.Arg is not null)
            .OrderBy(i => i.Node.Binding.Id)
            .ToImmutableArray();
}