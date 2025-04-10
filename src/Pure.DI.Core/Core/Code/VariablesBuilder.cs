// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

sealed class VariablesBuilder(
    IVariableNameProvider variableNameProvider,
    INodeInfo nodeInfo,
    CancellationToken cancellationToken)
    : IVariablesBuilder
{
    public Block Build(
        DependencyGraph dependencyGraph,
        IDictionary<MdBinding, Variable> map,
        DependencyNode rootNode,
        Injection rootInjection)
    {
        var graph = dependencyGraph.Graph;
        var blockId = 0;
        var transientId = 0;
        var blockMap = new Dictionary<(MdBinding, object?, int), Variable>();
        var rootBlock = new Block(blockId++, null, []);
        rootBlock.Statements.AddLast(
            GetVariable(
                dependencyGraph.Source,
                rootBlock,
                map,
                blockMap,
                rootNode,
                rootInjection,
                ref transientId));

        var blocks = new Stack<Block>();
        blocks.Push(rootBlock);
        while (blocks.TryPop(out var currentBlock))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var isFactoryWithOverrides = currentBlock.Current.Node.Factory is { HasOverrides: true };
            var stack = new Stack<IStatement>(currentBlock.Statements);
            while (stack.TryPop(out var currentStatement))
            {
                var firstStatement = currentBlock.Statements.Find(currentStatement) ?? currentBlock.Statements.First;
                switch (currentStatement)
                {
                    case Block block:
                        blocks.Push(block);
                        break;

                    case Variable variable:
                    {
                        var isAccumulator = IsAccumulator(variable, out var mdAccumulators);
                        IReadOnlyCollection<Dependency> dependencies = Array.Empty<Dependency>();
                        if (!isAccumulator)
                        {
                            if (!graph.TryGetInEdges(variable.Node, out dependencies)
                                || dependencies.Count == 0)
                            {
                                continue;
                            }
                        }

                        var path = new Dictionary<int, Variable>();
                        ICollection<Accumulator>? accumulators = null;
                        var isRoot = true;
                        foreach (var pathItem in currentStatement.GetPath())
                        {
                            var pathVar = pathItem.Current;
                            path[pathVar.Node.Binding.Id] = pathItem.Current;
                            if (!pathVar.IsLazy)
                            {
                                continue;
                            }

                            if (accumulators != null)
                            {
                                continue;
                            }

                            accumulators = pathVar.Node.Accumulators;
                            isRoot = false;
                        }

                        accumulators ??= rootNode.Accumulators;
                        if (isAccumulator)
                        {
                            var name = GetAccumulatorName(variable);
                            foreach (var mdAccumulator in mdAccumulators)
                            {
                                accumulators.Add(
                                    new Accumulator(
                                        isRoot,
                                        name,
                                        false,
                                        mdAccumulator.Type,
                                        mdAccumulator.Lifetime,
                                        mdAccumulator.AccumulatorType));
                            }
                        }

                        foreach (var dependency in dependencies.Where(i => i.IsResolved))
                        {
                            var hasCycle = path.TryGetValue(dependency.Source.Binding.Id, out var cycleVariable);
                            var isAlreadyCreated = false;
                            if (hasCycle)
                            {
                                isAlreadyCreated = nodeInfo.IsLazy(dependency.Source);
                                if (isAlreadyCreated)
                                {
                                    foreach (var pathVariable in path)
                                    {
                                        pathVariable.Value.HasCycledReference = true;
                                    }
                                }
                            }

                            var dependencyVariable = GetVariable(
                                dependencyGraph.Source,
                                currentBlock,
                                map,
                                blockMap,
                                dependency.Source with { Accumulators = accumulators },
                                dependency.Injection,
                                ref transientId,
                                cycleVariable);

                            dependencyVariable.Info.AddTargetNode(variable.Node);
                            var isBlock = isFactoryWithOverrides
                                          || dependency.Source.Lifetime is not Lifetime.Transient and not Lifetime.PerBlock
                                          || nodeInfo.IsDelegate(variable.Node)
                                          || nodeInfo.IsDelegate(dependency.Source);
                            if (isBlock)
                            {
                                var dependencyBlock = new Block(blockId++, currentStatement, []);
                                dependencyBlock.Statements.AddLast(dependencyVariable);
                                if (!isAlreadyCreated)
                                {
                                    blocks.Push(dependencyBlock);
                                }

                                if (!variable.IsLazy && !isFactoryWithOverrides)
                                {
                                    currentBlock.Statements.AddBefore(firstStatement, dependencyBlock);
                                }

                                variable.Args.Add(dependencyBlock);
                            }
                            else
                            {
                                if (!isAlreadyCreated)
                                {
                                    stack.Push(dependencyVariable);
                                }

                                if (!variable.IsLazy && !isFactoryWithOverrides)
                                {
                                    currentBlock.Statements.AddBefore(firstStatement, dependencyVariable);
                                }

                                variable.Args.Add(dependencyVariable);
                            }
                        }

                        break;
                    }
                }
            }
        }

        return rootBlock;
    }

    private static bool IsAccumulator(
        Variable variable,
        out IReadOnlyCollection<MdAccumulator> accumulators)
    {
        if (variable.Node.Construct?.Source is { Kind: MdConstructKind.Accumulator } construct)
        {
            accumulators = construct.State as IReadOnlyCollection<MdAccumulator> ?? ImmutableArray<MdAccumulator>.Empty;
            return true;
        }

        accumulators = ImmutableArray<MdAccumulator>.Empty;
        return false;
    }

    private static string GetAccumulatorName(Variable variable) =>
        $"accumulator{Names.Salt}{variable.Node.Binding.Id}";

    private Variable GetVariable(
        MdSetup setup,
        Block parentBlock,
        IDictionary<MdBinding, Variable> map,
        IDictionary<(MdBinding, object?, int), Variable> blockMap,
        DependencyNode node,
        in Injection injection,
        ref int transientId,
        Variable? cycleVariable = null)
    {
        if (cycleVariable is not null)
        {
            return cycleVariable with
            {
                Parent = parentBlock,
                Injection = injection,
                Args = new List<IStatement>(),
                HasCycle = true
            };
        }

        if (node.Arg is null)
        {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (node.Lifetime)
            {
                case Lifetime.Transient:
                {
                    var transientVariable = new Variable(variableNameProvider, setup, parentBlock, transientId++, node, injection, new List<IStatement>(), new VariableInfo(), nodeInfo.IsLazy(node), false);
                    if (node.Construct?.Source.Kind == MdConstructKind.Accumulator)
                    {
                        transientVariable.VariableCode = GetAccumulatorName(transientVariable);
                    }

                    return transientVariable;
                }

                case Lifetime.PerBlock:
                {
                    var perBlockKey = (node.Binding, injection.Tag, parentBlock.Id);
                    if (blockMap.TryGetValue(perBlockKey, out var blockVariable))
                    {
                        return blockVariable with
                        {
                            Parent = parentBlock,
                            Injection = injection,
                            Args = new List<IStatement>()
                        };
                    }

                    blockVariable = new Variable(variableNameProvider, setup, parentBlock, transientId++, node, injection, new List<IStatement>(), new VariableInfo(), nodeInfo.IsLazy(node), false);
                    blockMap.Add(perBlockKey, blockVariable);
                    return blockVariable;
                }
            }
        }

        var key = node.Binding;
        if (map.TryGetValue(key, out var variable))
        {
            variable.Info.AddRef(parentBlock);
            return variable with
            {
                Parent = parentBlock,
                Injection = injection,
                Args = new List<IStatement>()
            };
        }

        variable = new Variable(variableNameProvider, setup, parentBlock, node.Binding.Id, node, injection, new List<IStatement>(), new VariableInfo(), nodeInfo.IsLazy(node), false);
        map.Add(key, variable);
        return variable;
    }
}