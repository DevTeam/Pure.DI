namespace Pure.DI.Core;

internal class CodeBuilder: CodeGraphWalker<BuildContext>, IBuilder<DependencyGraph, Resolvers>
{
    private readonly IVarIdGenerator _idGenerator;
    private static readonly string IndentPrefix = new Indent(1).ToString();
    private static readonly BuildContext RootContext = new(ImmutableDictionary<MdBinding, Variable>.Empty, new LinesBuilder());
    private readonly Dictionary<Compilation, INamedTypeSymbol?> _disposableTypes = new();
    private readonly Dictionary<Root, ImmutableArray<Line>> _lines = new();

    public CodeBuilder(IVarIdGenerator idGenerator)
        : base(idGenerator) =>
        _idGenerator = idGenerator;

    public Resolvers Build(
        DependencyGraph dependencyGraph,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<MdBinding, Variable>();
        VisitGraph(RootContext, dependencyGraph, variables, cancellationToken);
        return new Resolvers(
            variables.ToImmutableDictionary(i => i.Value.Node, i => i.Value.Name),
            _lines.ToImmutableDictionary(),
            variables.Values.Count(IsDisposable)
        );
    }

    public override void VisitRoot(
        BuildContext context,
        DependencyGraph dependencyGraph,
        IDictionary<MdBinding, Variable> variables,
        Root root,
        CancellationToken cancellationToken)
    {
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
        DpImplementation implementation,
        CancellationToken cancellationToken)
    {
        var args = string.Join(", ", instantiation.Arguments.Select(i => i.Name));
        var newStatement = $"new {instantiation.Target.Node.Type}({args})";
        var justReturn = context.IsRootContext && instantiation.Target == root && instantiation.Target.Node.Lifetime != Lifetime.Singleton;
        var statement = GenerateFinalStatement(instantiation.Target, newStatement, justReturn);
        context.Code.AppendLine(statement);

        instantiation.Target.IsCreated = true;
        base.VisitImplementation(context, dependencyGraph, root, block, instantiation, implementation, cancellationToken);
    }

    public override void VisitArg(
        BuildContext context,
        DependencyGraph dependencyGraph,
        Variable rootVariable,
        Block block,
        Instantiation instantiation,
        DpArg dpArg,
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
        DpFactory factory,
        CancellationToken cancellationToken)
    {
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
                var argCodeBuilder = new CodeBuilder(_idGenerator);
                foreach (var arg in argsByContext)
                {
                    var argBuildContext = new BuildContext(context.Variables, new LinesBuilder(), false);
                    var rootVariable = CreateVariable(argBuildContext.Variables, arg.argument.Node);
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
}