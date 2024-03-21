// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class VariablesBuilder(CancellationToken cancellationToken)
    : IVariablesBuilder
{
    public Block Build(
        IGraph<DependencyNode, Dependency> graph,
        IDictionary<MdBinding, Variable> map,
        DependencyNode rootNode,
        Injection rootInjection)
    {
        var blockId = 0;
        var blockMap = new Dictionary<(MdBinding, int), Variable>();
        var rootBlock = new Block(blockId++,default, []);
        var transientId = 0;
        var rootVar = GetVariable(rootBlock, map, blockMap, rootNode, rootInjection, ref transientId, false);
        rootBlock.Statements.AddFirst(rootVar);
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
                        var isAccumulator = IsAccumulator(variable, out var construct);
                        IReadOnlyCollection<Dependency> dependencies = Array.Empty<Dependency>();
                        if (!isAccumulator)
                        {
                            if (!graph.TryGetInEdges(variable.Node, out dependencies)
                                || dependencies.Count == 0)
                            {
                                continue;
                            }
                        }

                        var pathIds = new HashSet<int>();
                        var hasLazy = false;
                        ICollection<Accumulator>? accumulators = default;
                        var isRoot = true;
                        foreach (var pathItem in currentStatement.GetPath())
                        {
                            var pathVar = pathItem.Current;
                            pathIds.Add(pathVar.Node.Binding.Id);
                            if (!pathVar.IsLazy)
                            {
                                continue;
                            }
                            
                            hasLazy = true;
                            if (accumulators != default)
                            {
                                continue;
                            }

                            accumulators = pathVar.Node.Accumulators;
                            isRoot = false;
                        }

                        accumulators ??= rootNode.Accumulators;
                        if (isAccumulator)
                        {
                            accumulators.Add(new Accumulator(isRoot, GetAccumulatorName(variable), false, construct.ElementType, construct.Type)); 
                        }

                        foreach (var (isDepResolved, depNode, depInjection, _) in dependencies)
                        {
                            if (!isDepResolved)
                            {
                                continue;
                            }

                            var hasCycle = pathIds.Contains(depNode.Binding.Id);
                            var isAlreadyCreated = false;
                            if (hasCycle)
                            {
                                if (!hasLazy)
                                {
                                    var pathStr = string.Join(" <-- ", currentStatement.GetPath().Reverse().Select(i => i.Current.Node).Concat(Enumerable.Repeat(depNode, 1)).Select(i => i.Type));
                                    throw new CompileErrorException($"Cyclic dependency has been found: {pathStr}.", depNode.Binding.Source.GetLocation(), LogId.ErrorCyclicDependency);
                                }

                                isAlreadyCreated = depNode.IsLazy();
                            }

                            var depVariable = GetVariable(currentBlock, map, blockMap, depNode, depInjection, ref transientId, hasCycle);
                            var isBlock = depNode.Lifetime is not Lifetime.Transient and not Lifetime.PerBlock || variable.Node.IsDelegate();
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

    private static bool IsAccumulator(Variable variable, out MdConstruct mdConstruct)
    {
        if(variable.Node.Construct?.Source is { Kind: MdConstructKind.Accumulator } construct)
        {
            mdConstruct = construct;
            return true;
        }

        mdConstruct = default;
        return false;
    }

    private static string GetAccumulatorName(Variable variable) => 
        $"accumulator{Names.Salt}{variable.Node.Binding.Id}";

    private static Variable GetVariable(
        Block parentBlock,
        IDictionary<MdBinding, Variable> map,
        IDictionary<(MdBinding, int), Variable> blockMap,
        DependencyNode node,
        in Injection injection,
        ref int transientId,
        bool hasCycle)
    {
        if (!node.IsArg())
        {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (node.Lifetime)
            {
                case Lifetime.Transient:
                {
                    var transientVariable = new Variable(parentBlock, transientId++, node, injection, new List<IStatement>(), new VariableInfo(), node.IsLazy(), hasCycle);
                    if (node.Construct?.Source.Kind == MdConstructKind.Accumulator)
                    {
                        transientVariable.VariableCode = GetAccumulatorName(transientVariable);
                    }

                    return transientVariable;
                }

                case Lifetime.PerBlock:
                {
                    var perBlockKey = (node.Binding, parentBlock.Id);
                    if (blockMap.TryGetValue(perBlockKey, out var blockVariable))
                    {
                        return blockVariable with
                        {
                            Parent = parentBlock,
                            Injection = injection,
                            Args = new List<IStatement>(),
                            HasCycle = hasCycle
                        };
                    }
                
                    blockVariable = new Variable(parentBlock, transientId++, node, injection, new List<IStatement>(), new VariableInfo(), node.IsLazy(), hasCycle);
                    blockMap.Add(perBlockKey, blockVariable);
                    return blockVariable;
                }
            }
        }

        if (map.TryGetValue(node.Binding, out var variable))
        {
            variable.Info.AddRef(parentBlock);
            return variable with
            {
                Parent = parentBlock,
                Injection = injection,
                Args = new List<IStatement>(),
                HasCycle = hasCycle
            };
        }

        variable = new Variable(parentBlock, node.Binding.Id, node, injection, new List<IStatement>(), new VariableInfo(), node.IsLazy(), hasCycle);
        map.Add(node.Binding, variable);
        return variable;
    }
}