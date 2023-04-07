// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
namespace Pure.DI.Core.CSharp;

internal class CompositionBuilder: CodeGraphWalker<BuildContext>, IBuilder<DependencyGraph, CompositionCode>
{
    private static readonly string IndentPrefix = new Indent(1).ToString();
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
        var isThreadSafe = dependencyGraph.Source.Settings.GetState(Setting.ThreadSafe, SettingState.On) == SettingState.On;
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

    public override void VisitRoot(
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

    public override void VisitBlock(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Block block,
        CancellationToken cancellationToken)
    {
        var initBlock = false;
        if (block.Root is { IsCreated: false, Node.Lifetime: Lifetime.Singleton })
        {
            StartSingletonInitBlock(context, block.Root);
            initBlock = true;
        }

        base.VisitBlock(context, dependencyGraph, root, block, cancellationToken);

        if (initBlock)
        {
            FinishSingletonInitBlock(context, block.Root);
            if (context.IsRootContext && block.Root == root)
            {
                context.Code.AppendLines(GenerateReturnStatements(context, block.Root));
            }
        }
    }

    public override void VisitImplementation(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Block block,
        Instantiation instantiation,
        in DpImplementation implementation,
        CancellationToken cancellationToken)
    {
        base.VisitImplementation(context, dependencyGraph, root, block, instantiation, implementation, cancellationToken);
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
    }

    public override void VisitConstructor(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Block block,
        Instantiation instantiation,
        in DpImplementation implementation,
        in DpMethod constructor,
        in ImmutableArray<Variable> constructorArguments,
        in ImmutableArray<(Variable InitOnlyVariable, DpProperty InitOnlyProperty)> initOnlyProperties,
        CancellationToken cancellationToken)
    {
        base.VisitConstructor(context, dependencyGraph, root, block, instantiation, implementation, constructor, constructorArguments, initOnlyProperties, cancellationToken);
        
        string InjectVar(Variable variable) => this.Inject(context, variable);
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

    public override void VisitField(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Block block,
        Instantiation instantiation,
        in DpImplementation implementation,
        in DpField field,
        Variable fieldVariable,
        CancellationToken cancellationToken)
    {
        base.VisitField(context, dependencyGraph, root, block, instantiation, implementation, field, fieldVariable, cancellationToken);
        context.Code.AppendLine($"{instantiation.Target.Name}.{field.Field.Name} = {Inject(context, fieldVariable)};");
    }

    public override void VisitProperty(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Block block,
        Instantiation instantiation,
        in DpImplementation implementation,
        in DpProperty property,
        Variable propertyVariable,
        CancellationToken cancellationToken)
    {
        base.VisitProperty(context, dependencyGraph, root, block, instantiation, implementation, property, propertyVariable, cancellationToken);
        context.Code.AppendLine($"{instantiation.Target.Name}.{property.Property.Name} = {Inject(context, propertyVariable)};");
    }

    public override void VisitMethod(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Block block,
        Instantiation instantiation,
        in DpImplementation implementation,
        in DpMethod method,
        in ImmutableArray<Variable> methodArguments,
        CancellationToken cancellationToken)
    {
        base.VisitMethod(context, dependencyGraph, root, block, instantiation, implementation, method, methodArguments, cancellationToken);

        string Inject(Variable variable) => this.Inject(context, variable);
        var args = string.Join(", ", methodArguments.Select(Inject));
        context.Code.AppendLine($"{instantiation.Target.Name}.{method.Method.Name}({args});");
    }

    public override void VisitArg(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable rootVariable,
        Block block,
        Instantiation instantiation,
        in DpArg dpArg,
        CancellationToken cancellationToken)
    {
        if (context.IsRootContext && instantiation.Target == rootVariable)
        {
            context.Code.AppendLines(GenerateReturnStatements(context, instantiation.Target));
        }

        base.VisitArg(context, dependencyGraph, rootVariable, block, instantiation, dpArg, cancellationToken);
    }

    public override void VisitEnumerableConstruct(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Block block,
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
    
    public override void VisitArrayConstruct(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Block block,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation,
        CancellationToken cancellationToken)
    {
        if (instantiation.Target.IsCreated)
        {
            return;
        }

        string Inject(Variable variable) => this.Inject(context, variable);
        context.Code.AppendLine($"{instantiation.Target.InstanceType} {instantiation.Target.Name} = new {construct.Source.ElementType}[{instantiation.Arguments.Length.ToString()}] {{ {string.Join(", ", instantiation.Arguments.Select(Inject))} }};");
        context.Code.AppendLines(GenerateOnInstanceCreatedStatements(context, instantiation.Target));
        AddReturnStatement(context, root, instantiation);
        instantiation.Target.IsCreated = true;
    }
    
    public override void VisitSpanConstruct(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Block block,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation,
        CancellationToken cancellationToken)
    {
        if (instantiation.Target.IsCreated)
        {
            return;
        }
        
        string Inject(Variable variable) => this.Inject(context, variable);
        var createArray = $"{construct.Source.ElementType}[{instantiation.Arguments.Length.ToString()}] {{ {string.Join(", ", instantiation.Arguments.Select(Inject))} }}";
        var createInstance = 
            construct.Source.ElementType.IsValueType
            && construct.Binding.SemanticModel.Compilation.GetLanguageVersion() >= LanguageVersion.CSharp7_3
            && !IsJustReturn(context, root, instantiation) 
                ? $"stackalloc {createArray}"
                : $"new System.Span<{construct.Source.ElementType}>(new {createArray})";
        context.Code.AppendLine($"{instantiation.Target.InstanceType} {instantiation.Target.Name} = {createInstance};");
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

    public override void VisitFactory(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Block block,
        Instantiation instantiation,
        in DpFactory factory,
        CancellationToken cancellationToken)
    {
        if (instantiation.Target.IsCreated)
        {
            return;
        }

        var lambda = factory.Source.Factory;
        var initializers = new Dictionary<string, ImmutableArray<string>>();
        if (instantiation.Arguments.Any())
        {
            var injectsRewriter = new FactoryInjectsRewriter(factory);
            lambda = (SimpleLambdaExpressionSyntax)injectsRewriter.VisitSimpleLambdaExpression(factory.Source.Factory)!;
            var argsByContexts = injectsRewriter
                .Zip(instantiation.Arguments, (factoryInjection, argument) => (factoryInjection, argument))
                .GroupBy(i => i.factoryInjection.ContextId);

            var namesMap = new Dictionary<string, string>(); 
            foreach (var argsByContext in argsByContexts)
            {
                var argCodeBuilder = CreateChildBuilder();
                foreach (var arg in argsByContext)
                {
                    var argBuildContext = context with
                    {
                        Code = new LinesBuilder(),
                        IsRootContext = false,
                        ContextTag = root.Injection.Tag
                    };

                    var rootVariable = CreateVariable(dependencyGraph, argBuildContext.Variables, arg.argument.Node, arg.argument.Injection);
                    argCodeBuilder.VisitRootVariable(argBuildContext, dependencyGraph, context.Variables, rootVariable, cancellationToken);
                    namesMap.Add(arg.factoryInjection.VariableName, Inject(context, rootVariable));
                    initializers.Add($"{arg.factoryInjection.InjectionId};", argBuildContext.Code.ToImmutableArray());
                }
            }
            
            var namesRewriter = new FactoryNamesRewriter(namesMap);
            lambda = (SimpleLambdaExpressionSyntax)namesRewriter.VisitSimpleLambdaExpression(lambda)!;
        }
        
        var justReturn = context.IsRootContext && instantiation.Target == root && instantiation.Target.Node.Lifetime != Lifetime.Singleton;
        var lines = new List<string>();
        if (lambda.Block is { } factoryBlock)
        {
            var finishMark = $"mark{_idGenerator.NextId.ToString()}{Variable.Postfix}";
            var blockRewriter = new FactoryBlockRewriter(instantiation.Target, finishMark);
            factoryBlock = (BlockSyntax)blockRewriter.VisitBlock(factoryBlock)!;
            var initStatements = factoryBlock.Statements.SelectMany(ConvertToLines);
            lines.Add($"{instantiation.Target.InstanceType} {instantiation.Target.Name};");
            lines.AddRange(initStatements);
            if (lines.Count > 0 && lines[^1].TrimStart() == $"goto {finishMark};")
            {
                lines.RemoveAt(lines.Count - 1);
            }
            else
            {
                lines.Add($"{finishMark}:;");
            }
            
            if (justReturn)
            {
                lines.AddRange(GenerateReturnStatements(context, instantiation.Target));
            }
        }
        else
        {
            var initStatements = ConvertToLines(lambda.Body).ToList();
            var initStatement = initStatements[0];
            initStatements.RemoveAt(0);
            var position = 0;
            foreach (var line in GenerateDeclareStatements(instantiation.Target, initStatement, ""))
            {
                initStatements.Insert(position++, line);
            }
            initStatements[^1] += ";";
            initStatements.AddRange(GenerateOnInstanceCreatedStatements(context, instantiation.Target));

            if (justReturn)
            {
                initStatements.AddRange(GenerateReturnStatements(context, instantiation.Target));
            }
            
            lines.AddRange(initStatements);
        }

        if (initializers.Any())
        {
            var newLines = new List<string>(lines.Count);
            foreach (var line in lines)
            {
                var trimmedLine = line.TrimStart();
                var indent = new string(' ', line.Length - trimmedLine.Length);
                if (initializers.TryGetValue(trimmedLine, out var initLines))
                {
                    newLines.AddRange(initLines.Select(i => indent + i));
                }
                else
                {
                    newLines.Add(line);
                }
            }

            lines = newLines;
        }

        foreach (var line in lines)
        {
            context.Code.AppendLine(line);
        }

        instantiation.Target.IsCreated = true;
        base.VisitFactory(context, dependencyGraph, root, block, instantiation, factory, cancellationToken);
    }

    private static IEnumerable<string> ConvertToLines(SyntaxNode node)
    {
        if (node is BlockSyntax block)
        {
            return block.Statements.SelectMany(ConvertToLines);
        }

        return node.NormalizeWhitespace(IndentPrefix)
            .ToString()
            .Split(Environment.NewLine);
    }

    private static void StartSingletonInitBlock(BuildContext context, Variable variable)
    {
        var checkExpression = variable.InstanceType.IsValueType switch
        {
            true => $"!{variable.Name}Created",
            false => $"System.Object.ReferenceEquals({variable.Name}, null)"
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
    
    private static IEnumerable<string> GenerateDeclareStatements(Variable variable, string instantiation, string lastChars = ";")
    {
        var declaration = variable.IsDeclared ? $"{variable.Name}" : $"{variable.InstanceType} {variable.Name}";
        yield return $"{declaration} = {instantiation}{lastChars}";
    }
    
    private static IEnumerable<string> GenerateOnInstanceCreatedStatements(BuildContext context, Variable variable)
    {
        if (variable.Node.Arg is not null)
        {
            yield break;
        }

        if (variable.Source.Source.Settings.GetState(Setting.OnInstanceCreation, SettingState.On) != SettingState.On)
        {
            yield break;
        }

        var tag = GetTag(context, variable);
        yield return $"{Constant.OnInstanceCreationMethodName}<{variable.InstanceType}>(ref {variable.Name}, {tag.TagToString()}, {variable.Node.Binding.Lifetime?.Lifetime.TagToString() ?? "null"})" + ";";
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

    private IEnumerable<string> GenerateReturnStatements(BuildContext context, Variable variable, string lastChars = ";")
    {
        yield return $"return {Inject(context, variable)}{lastChars}";
    }

    private string Inject(BuildContext context, Variable variable)
    {
        if (variable.Source.Source.Settings.GetState(Setting.OnDependencyInjection) != SettingState.On)
        {
            return variable.Name;
        }

        if (!_filter.IsMeetRegularExpression(
                variable.Source.Source,
                (Setting.OnDependencyInjectionImplementationTypeNameRegularExpression, variable.Node.Type.ToString()),
                (Setting.OnDependencyInjectionContractTypeNameRegularExpression, variable.Injection.Type.ToString()),
                (Setting.OnDependencyInjectionTagRegularExpression, variable.Injection.Tag.TagToString())))
        {
            return variable.Name;
        }
        
        var tag = GetTag(context, variable);
        return $"{Constant.OnDependencyInjectionMethodName}<{variable.ContractType}>({variable.Name}, {tag.TagToString()}, {variable.Node.Binding.Lifetime?.Lifetime.TagToString() ?? "null"})";
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
            .Where(i => Equals(i.Node.Binding.Lifetime?.Lifetime, Lifetime.Singleton))
            .OrderBy(i => i.Node.Binding.Id)
            .ToImmutableArray();

    private static ImmutableArray<Variable> GetArgs(in ImmutableArray<Variable> fields) =>
        fields
            .Where(i => i.Node.Arg is not null)
            .OrderBy(i => i.Node.Binding.Id)
            .ToImmutableArray();
}