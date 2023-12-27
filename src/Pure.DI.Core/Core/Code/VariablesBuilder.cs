// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class VariablesBuilder : IVariablesBuilder
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
            var stack = new Stack<IStatement>(currentBlock.Statements);
            while (stack.TryPop(out var currentStatement))
            {
                var path = currentStatement.GetPath().ToArray();
                var hasLazy = path.Any(i => i.Current.IsLazy);
                switch (currentStatement)
                {
                    case Block block:
                        blocks.Push(block);
                        break;
                    
                    case Variable variable:
                    {
                        if (!graph.TryGetInEdges(variable.Node, out var dependencies))
                        {
                            continue;
                        }

                        foreach (var (isDepResolved, depNode, depInjection, _) in dependencies)
                        {
                            if (!isDepResolved)
                            {
                                continue;
                            }

                            var cycle = path.FirstOrDefault(i => i.Current.Node.Binding == depNode.Binding);
                            var hasCycle = cycle is not null;
                            var isAlreadyCreated = false;
                            if (hasCycle)
                            {
                                if (!hasLazy)
                                {
                                    var pathStr = string.Join(" <-- ", path.Reverse().Select(i => i.Current.Node).Concat(Enumerable.Repeat(depNode, 1)).Select(i => i.Type));
                                    throw new CompileErrorException($"Cyclic dependency has been found: {pathStr}.", depNode.Binding.Source.GetLocation(), LogId.ErrorCyclicDependency);
                                }

                                isAlreadyCreated = depNode.IsLazy();
                            }

                            var depVariable = GetVariable(currentBlock, map, blockMap, depNode, depInjection, ref transientId, hasCycle);
                            var isBlock = depNode.Lifetime is not Lifetime.Transient and not Lifetime.PerBlock || variable.IsLazy;
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
    
    private static Variable GetVariable(
        Block parentBlock,
        IDictionary<MdBinding, Variable> map,
        IDictionary<(MdBinding, int), Variable> blockMap,
        in DependencyNode node,
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
                    return new Variable(parentBlock, transientId++, node, injection, new List<IStatement>(), new VariableInfo(), node.IsLazy(), hasCycle);
                
                case Lifetime.PerBlock:
                {
                    if (blockMap.TryGetValue((node.Binding, parentBlock.Id), out var blockVariable))
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
                    blockMap.Add((node.Binding, parentBlock.Id), blockVariable);
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