namespace Pure.DI.Core.CSharp;

internal class CodeComposerBuilder: CodeGraphWalker<BuildContext>, IBuilder<DependencyGraph, ComposerCode>
{
    private readonly IVarIdGenerator _idGenerator;
    private static readonly string IndentPrefix = new Indent(1).ToString();
    private static readonly BuildContext RootContext = new(ImmutableDictionary<MdBinding, Variable>.Empty, new LinesBuilder());
    private readonly Dictionary<Compilation, INamedTypeSymbol?> _disposableTypes = new();
    private readonly Dictionary<Root, ImmutableArray<Line>> _lines = new();

    public CodeComposerBuilder(IVarIdGenerator idGenerator)
        : base(idGenerator) => _idGenerator = idGenerator;

    public ComposerCode Build(
        DependencyGraph dependencyGraph,
        CancellationToken cancellationToken)
    {
        var composerTypeNameParts = dependencyGraph.Source.ComposerTypeName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var className = composerTypeNameParts.Last();
        var ns = string.Join('.', composerTypeNameParts.Take(composerTypeNameParts.Length - 1));
        if (string.IsNullOrWhiteSpace(ns))
        {
            ns = dependencyGraph.Source.Namespace;
        }
        
        var usingDirectives = dependencyGraph.Source.UsingDirectives.ToHashSet();
        var variables = new Dictionary<MdBinding, Variable>();
        VisitGraph(RootContext, dependencyGraph, variables, cancellationToken);
        var fields = variables.Select(i => new Field(i.Value.Node, i.Value.Name)).ToImmutableArray();
        return new ComposerCode(
            className,
            ns,
            usingDirectives.OrderBy(i => i).ToImmutableArray(),
            GetSingletons(fields),
            GetArgs(fields),
            _lines.Select(i => i.Key with { Lines = i.Value }).ToImmutableArray(),
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
        if (_lines.ContainsKey(root))
        {
            return;
        }

        var newContext = new BuildContext(variables, new LinesBuilder());
        base.VisitRoot(newContext, dependencyGraph, variables, root, cancellationToken);
        _lines.Add(root, newContext.Code.Lines.ToImmutableArray());
    }

    public override void VisitBlock(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Block block,
        CancellationToken cancellationToken)
    {
        if (block.Root.IsCreated && !context.IsRootContext)
        {
            return;
        }
        
        switch (block.Root.Node.Lifetime)
        {
            case Lifetime.Singleton:
                StartSingletonInitBlock(context.Code, block.Root);
                break;
            
            case Lifetime.PerResolve:
            case Lifetime.Transient:
            default:
                break;
        }
        
        base.VisitBlock(context, dependencyGraph, root, block, cancellationToken);
        
        switch (block.Root.Node.Lifetime)
        {
            case Lifetime.Singleton:
                FinishSingletonInitBlock(context.Code, block.Root);
                if (context.IsRootContext && block.Root == root)
                {
                    context.Code.AppendLine(GenerateFinalStatement(block.Root, block.Root.Name, true));
                }

                break;
            
            case Lifetime.PerResolve:
            case Lifetime.Transient:
            default:
                break;
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
        var justReturn = context.IsRootContext && instantiation.Target == root && instantiation.Target.Node.Lifetime != Lifetime.Singleton;
        if (justReturn)
        {
            context.Code.AppendLine(GenerateFinalStatement(instantiation.Target, instantiation.Target.Name, justReturn));
        }

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
        var args = string.Join(", ", constructorArguments.Select(i => i.Name));
        var newStatement = $"new {instantiation.Target.Node.Type}({args})";
        var statement = GenerateFinalStatement(instantiation.Target, newStatement, false, initOnlyProperties.Any() ? "" : ";");
        context.Code.AppendLine(statement);
        if (initOnlyProperties.Any())
        {
            context.Code.AppendLine("{");
            using (context.Code.Indent())
            {
                for (var index = 0; index < initOnlyProperties.Length; index++)
                {
                    var property = initOnlyProperties[index];
                    context.Code.AppendLine($"{property.InitOnlyProperty.Property.Name} = {property.InitOnlyVariable.Name}{(index < initOnlyProperties.Length - 1 ? "," : "")}");
                }
            }
            context.Code.AppendLine("};");
        }
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
        context.Code.AppendLine($"{instantiation.Target.Name}.{field.Field.Name} = {fieldVariable.Name};");
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
        context.Code.AppendLine($"{instantiation.Target.Name}.{property.Property.Name} = {propertyVariable.Name};");
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
        var args = string.Join(", ", methodArguments.Select(i => i.Name));
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
            context.Code.AppendLine(GenerateFinalStatement(instantiation.Target, instantiation.Target.Name, true));
        }

        base.VisitArg(context, dependencyGraph, rootVariable, block, instantiation, dpArg, cancellationToken);
    }

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
                var argCodeBuilder = new CodeComposerBuilder(_idGenerator);
                foreach (var arg in argsByContext)
                {
                    var argBuildContext = new BuildContext(context.Variables, new LinesBuilder(), false);
                    var rootVariable = CreateVariable(argBuildContext.Variables, arg.argument.Node, arg.argument.Injection);
                    argCodeBuilder.VisitRootVariable(argBuildContext, dependencyGraph, context.Variables, rootVariable, cancellationToken);
                    namesMap.Add(arg.factoryInjection.VariableName, rootVariable.Name);
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
            var finishMark = $"mark{_idGenerator.NextId}{Variable.Postfix}";
            var blockRewriter = new FactoryBlockRewriter(instantiation.Target, finishMark);
            factoryBlock = (BlockSyntax)blockRewriter.VisitBlock(factoryBlock)!;
            var initStatements = factoryBlock.Statements.SelectMany(ConvertToLines);
            lines.Add($"{instantiation.Target.Type} {instantiation.Target.Name};");
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
                lines.Add(GenerateFinalStatement(instantiation.Target, instantiation.Target.Name, true));
            }
        }
        else
        {
            var initStatements = ConvertToLines(lambda.Body).ToArray();
            initStatements[0] = GenerateFinalStatement(instantiation.Target, initStatements[0], justReturn, "");
            initStatements[^1] += ";";
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

        return node.NormalizeWhitespace(IndentPrefix).ToString().Split(Environment.NewLine);
    }

    private static void StartSingletonInitBlock(LinesBuilder code, Variable variable)
    {
        var checkExpression = variable.Node.Type.IsValueType switch
        {
            true => $"!{variable.Name}Created",
            false => $"System.Object.ReferenceEquals({variable.Name}, null)"
        };

        code.AppendLine($"if ({checkExpression})");
        code.AppendLine("{");
        code.IncIndent();
        code.AppendLine($"lock ({Variable.DisposablesFieldName})");
        code.AppendLine("{");
        code.IncIndent();
        code.AppendLine($"if ({checkExpression})");
        code.AppendLine("{");
        code.IncIndent();
    }

    private void FinishSingletonInitBlock(LinesBuilder code, Variable variable)
    {
        if (IsDisposable(variable))
        {
            code.AppendLine($"{Variable.DisposablesFieldName}[{Variable.DisposeIndexFieldName}++] = {variable.Name};");
        }
        
        if (variable.Node.Type.IsValueType)
        {
            code.AppendLine($"{variable.Name}Created = true;");
        }

        code.DecIndent();
        code.AppendLine("}");
        code.DecIndent();
        code.AppendLine("}");
        code.DecIndent();
        code.AppendLine("}");
        code.AppendLine();
    }
    
    private static string GenerateFinalStatement(Variable variable, string instantiation, bool justReturn, string lastChars = ";")
    {
        if (justReturn)
        {
            return $"return {instantiation}{lastChars}";
        }

        var declaration = variable.IsDeclared ? $"{variable.Name}" : $"{variable.Type} {variable.Name}";
        return $"{declaration} = {instantiation}{lastChars}";
    }
    
    private bool IsDisposable(Variable variable)
    {
        var compilation = variable.Node.Binding.SemanticModel.Compilation;
        if (!_disposableTypes.TryGetValue(compilation, out var disposableType))
        {
            disposableType = compilation.GetTypeByMetadataName("System.IDisposable");
            _disposableTypes.Add(compilation, disposableType);
        }
        
        return disposableType is { }
               && variable.Node.Type.AllInterfaces.Any(i => disposableType.Equals(i, SymbolEqualityComparer.Default));
    }
    
    private static ImmutableArray<Field> GetSingletons(in ImmutableArray<Field> fields) =>
        fields
            .Where(i => Equals(i.Node.Binding.Lifetime?.Lifetime, Lifetime.Singleton))
            .OrderBy(i => i.Node.Binding.Id)
            .ToImmutableArray();

    private static ImmutableArray<Field> GetArgs(in ImmutableArray<Field> fields) =>
        fields
            .Where(i => i.Node.Arg is { })
            .OrderBy(i => i.Node.Binding.Id)
            .ToImmutableArray();
}