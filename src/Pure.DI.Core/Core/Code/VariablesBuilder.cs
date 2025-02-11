// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

internal sealed class VariablesBuilder(
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
        rootBlock.Statements.AddFirst(
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

            var stack = new Stack<IStatement>(currentBlock.Statements);
            while (stack.TryPop(out var currentStatement))
            {
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

                        foreach (var (isDepResolved, depNode, depInjection, _) in dependencies)
                        {
                            if (!isDepResolved)
                            {
                                continue;
                            }

                            var hasCycle = path.TryGetValue(depNode.Binding.Id, out var cycleVariable);
                            var isAlreadyCreated = false;
                            if (hasCycle)
                            {
                                isAlreadyCreated = nodeInfo.IsLazy(depNode);
                                if (isAlreadyCreated)
                                {
                                    foreach (var pathVariable in path)
                                    {
                                        pathVariable.Value.HasCycledReference = true;
                                    }
                                }
                            }

                            var depVariable = GetVariable(
                                dependencyGraph.Source,
                                currentBlock,
                                map,
                                blockMap,
                                depNode with { Accumulators = accumulators },
                                depInjection,
                                ref transientId,
                                cycleVariable);
                            
                            depVariable.Info.AddTargetNode(variable.Node);

                            var isBlock = depNode.Lifetime is not Lifetime.Transient and not Lifetime.PerBlock
                                          || nodeInfo.IsDelegate(variable.Node)
                                          || nodeInfo.IsDelegate(depNode);
                            if (isBlock)
                            {
                                var depBlock = new Block(blockId++, currentStatement, []);
                                depBlock.Statements.AddFirst(depVariable);
                                if (!isAlreadyCreated)
                                {
                                    blocks.Push(depBlock);
                                }

                                if (!variable.IsLazy)
                                {
                                    currentBlock.Statements.AddFirst(depBlock);
                                }

                                variable.Args.Add(depBlock);
                            }
                            else
                            {
                                if (!isAlreadyCreated)
                                {
                                    stack.Push(depVariable);
                                }

                                if (!variable.IsLazy)
                                {
                                    currentBlock.Statements.AddFirst(depVariable);
                                }

                                variable.Args.Add(depVariable);
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
                    var transientVariable = new Variable(setup, parentBlock, transientId++, node, injection, new List<IStatement>(), new VariableInfo(), nodeInfo.IsLazy(node), false);
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

                    blockVariable = new Variable(setup, parentBlock, transientId++, node, injection, new List<IStatement>(), new VariableInfo(), nodeInfo.IsLazy(node), false);
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

        variable = new Variable(setup, parentBlock, node.Binding.Id, node, injection, new List<IStatement>(), new VariableInfo(), nodeInfo.IsLazy(node), false);
        map.Add(key, variable);
        return variable;
    }
}