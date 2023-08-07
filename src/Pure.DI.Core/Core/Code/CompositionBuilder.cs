// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
namespace Pure.DI.Core.Code;

internal sealed class CompositionBuilder: CodeGraphWalker<BuildContext>, IBuilder<DependencyGraph, CompositionCode>
{
    private static readonly string InjectionStatement = $"{Variable.InjectionMarker};";
    private readonly ILogger<CompositionBuilder> _logger;
    private readonly Func<IVarIdGenerator> _idGeneratorFactory;
    private readonly IFilter _filter;
    private readonly CancellationToken _cancellationToken;
    private readonly Dictionary<Compilation, INamedTypeSymbol?> _disposableTypes = new();
    private readonly Dictionary<Root, ImmutableArray<Line>> _roots = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public CompositionBuilder(
        ILogger<CompositionBuilder> logger,
        Func<IVarIdGenerator> idGeneratorFactory,
        IFilter filter,
        CancellationToken cancellationToken)
    {
        _logger = logger;
        _idGeneratorFactory = idGeneratorFactory;
        _filter = filter;
        _cancellationToken = cancellationToken;
    }
    
    private CompositionBuilder CreateChildBuilder(BuildContext context) => new(_logger, () => context.IdGenerator, _filter, _cancellationToken);

    public CompositionCode Build(DependencyGraph dependencyGraph)
    {
        _roots.Clear();
        var variables = new Dictionary<MdBinding, Variable>();
        var isThreadSafe = dependencyGraph.Source.Hints.GetHint(Hint.ThreadSafe, SettingState.On) == SettingState.On;
        var context = new BuildContext(
            isThreadSafe,
            ImmutableDictionary<MdBinding, Variable>.Empty,
            new LinesBuilder(),
            _idGeneratorFactory());
        VisitGraph(context, dependencyGraph, variables, _cancellationToken);
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
        var hasRootArgs = newContext.Variables.Values.Any(i => i.Node.Arg?.Source.Kind == ArgKind.Root);
        _roots.Add(root with{ HasRootArgs = hasRootArgs }, newContext.Code.Lines.ToImmutableArray());
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
        if (!instantiation.Target.IsCreationRequired(instantiation.Target.Node))
        {
            return;
        }
        
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
        in ImmutableArray<(Variable RequiredVariable, DpField RequiredField)> requiredFields,
        in ImmutableArray<(Variable RequiredVariable, DpProperty RequiredProperty)> requiredProperties)
    {
        base.VisitConstructor(context, instantiation, implementation, constructor, constructorArguments, requiredFields, requiredProperties);

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

        var required = requiredFields.Select(i => (Variable: i.RequiredVariable, i.RequiredField.Field.Name))
            .Concat(requiredProperties.Select(i => (Variable: i.RequiredVariable, i.RequiredProperty.Property.Name)))
            .ToArray();
            
        var hasRequired = required.Any();
        context.Code.AppendLines(GenerateDeclareStatements(instantiation.Target, newStatement, hasRequired ? "" : ";"));
        if (hasRequired)
        {
            context.Code.AppendLine("{");
            using (context.Code.Indent())
            {
                for (var index = 0; index < required.Length; index++)
                {
                    var (variable, name) = required[index];
                    context.Code.AppendLine($"{name} = {Inject(context, variable)}{(index < required.Length - 1 ? "," : "")}");
                }
            }

            context.Code.AppendLine("};");
        }
        

        context.Code.AppendLines(GenerateOnInstanceCreatedStatements(context, instantiation.Target));
        return;

        string InjectVar(Variable variable) => Inject(context, variable);
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

        var args = string.Join(", ", methodArguments.Select(InjectVariable));
        context.Code.AppendLine($"{instantiation.Target.Name}.{method.Method.Name}({args});");
        return;

        string InjectVariable(Variable variable) => Inject(context, variable);
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
        if (!instantiation.Target.IsCreationRequired(instantiation.Target.Node))
        {
            return;
        }

        var localFuncName = $"LocalFunc_{instantiation.Target.Name}";
        context.Code.AppendLine($"{instantiation.Target.InstanceType} {localFuncName}()");
        context.Code.AppendLine("{");
        using (context.Code.Indent())
        {
            if (instantiation.Arguments.Any())
            {
                var isFirst = true;
                foreach (var arg in instantiation.Arguments)
                {
                    if (!isFirst)
                    {
                        context.Code.AppendLine();
                    }

                    if (arg.Node.Lifetime != Lifetime.PerResolve)
                    {
                        arg.AllowCreation();
                        VisitRootVariable(context with { IsRootContext = false }, dependencyGraph, context.Variables, arg, cancellationToken);
                    }

                    context.Code.AppendLine($"yield return {Inject(context, arg)};");
                    isFirst = false;
                }
            }
            else
            {
                context.Code.AppendLine("yield break;");
            }
        }

        context.Code.AppendLine("}");
        context.Code.AppendLine();
        context.Code.AppendLine($"var {instantiation.Target.Name} = {localFuncName}();");

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
        if (!instantiation.Target.IsCreationRequired(instantiation.Target.Node))
        {
            return;
        }

        context.Code.AppendLine($"var {instantiation.Target.Name} = new {construct.Source.ElementType}[{instantiation.Arguments.Length.ToString()}] {{ {string.Join(", ", instantiation.Arguments.Select(InjectVariable))} }};");
        context.Code.AppendLines(GenerateOnInstanceCreatedStatements(context, instantiation.Target));
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
        return;

        string InjectVariable(Variable variable) => Inject(context, variable);
    }

    protected override void VisitSpanConstruct(
        BuildContext context,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation)
    {
        if (!instantiation.Target.IsCreationRequired(instantiation.Target.Node))
        {
            return;
        }

        var createArray = $"{construct.Source.ElementType}[{instantiation.Arguments.Length.ToString()}] {{ {string.Join(", ", instantiation.Arguments.Select(InjectVariable))} }}";

        var isStackalloc = construct.Source.ElementType.IsValueType
            && construct.Binding.SemanticModel.Compilation.GetLanguageVersion() >= LanguageVersion.CSharp7_3
            && !IsJustReturn(context, root, instantiation);
        
        var createInstance = isStackalloc ? $"stackalloc {createArray}" : $"new {Constant.SystemNamespace}Span<{construct.Source.ElementType}>(new {createArray})";
        context.Code.AppendLine($"{(isStackalloc ? instantiation.Target.InstanceType : "var")} {instantiation.Target.Name} = {createInstance};");
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
        return;

        string InjectVariable(Variable variable) => Inject(context, variable);
    }

    protected override void VisitCompositionConstruct(
        BuildContext context,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation)
    {
        if (!instantiation.Target.IsCreationRequired(instantiation.Target.Node))
        {
            return;
        }

        context.Code.AppendLines(GenerateDeclareStatements(instantiation.Target, "this"));
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
    }

    protected override void VisitOnCannotResolve(BuildContext context, Variable root, in DpConstruct construct, Instantiation instantiation)
    {
        if (!instantiation.Target.IsCreationRequired(instantiation.Target.Node))
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
        if (!instantiation.Target.IsCreationRequired(instantiation.Target.Node))
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
        var finishLabel = $"label{GenerateId(context).ToString()}";
        var injections = new List<FactoryRewriter.Injection>();
        var factoryRewriter = new FactoryRewriter(factory, instantiation.Target, context.ContextTag, finishLabel, injections);
        var lambda = factoryRewriter.Rewrite(factory.Source.Factory);

        SyntaxNode syntaxNode = lambda.Block is not null
            ? lambda.Block
            : SyntaxFactory.ExpressionStatement((ExpressionSyntax)lambda.Body);

        var lines = syntaxNode.ToString().Split('\n');
        
        // Replaces injection markers by injection code
        var factoryCodeBuilder = CreateChildBuilder(context);
        using var resolvers = injections.Zip(instantiation.Arguments, (injection, argument) => (injection, argument)).GetEnumerator();
        var indent = new Indent(0);
        foreach (var line in lines)
        {
            if (line.Trim() == InjectionStatement && resolvers.MoveNext())
            {
                // When an injection marker
                var resolver = resolvers.Current;
                var injectedVariable = CreateVariable(context, dependencyGraph, context.Variables, resolver.argument.Node, resolver.argument.Injection);
                using (code.Indent(indent.Value))
                {
                    factoryCodeBuilder.VisitRootVariable(context, dependencyGraph, context.Variables, injectedVariable, cancellationToken);
                    code.AppendLine($"{(resolver.injection.DeclarationRequired ? "var " : "")}{resolver.injection.VariableName} = {Inject(context, injectedVariable)};");
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
        var declaration = variable.IsDeclared ? $"{variable.Name}" : $"var {variable.Name}";
        yield return new Line(0, $"{declaration} = {instantiation}{lastChars}");
    }
    
    private IEnumerable<Line> GenerateOnInstanceCreatedStatements(BuildContext context, Variable variable)
    {
        if (variable.Node.Arg is not null)
        {
            yield break;
        }

        if (variable.Source.Source.Hints.GetHint(Hint.OnNewInstance) != SettingState.On)
        {
            yield break;
        }

        if (!_filter.IsMeetRegularExpression(
                variable.Source.Source,
                (Hint.OnNewInstanceImplementationTypeNameRegularExpression, variable.Node.Type.ToString()),
                (Hint.OnNewInstanceTagRegularExpression, variable.Injection.Tag.ValueToString()),
                (Hint.OnNewInstanceLifetimeRegularExpression, variable.Node.Lifetime.ValueToString())))
        {
            yield break;
        }

        var tag = GetTag(context, variable);
        yield return new Line(0, $"{Constant.OnNewInstanceMethodName}<{variable.InstanceType}>(ref {variable.Name}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()})" + ";");
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

    protected override int GenerateId(BuildContext context) => 
        context.IdGenerator.NextId;

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