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
        var blockMap = new Dictionary<int, Variable>();
        var rootBlock = new Block(blockId++,default, new LinkedList<IStatement>());
        var transientId = 0;
        var rootVar = GetVariable(rootBlock, rootBlock, map, blockMap, rootNode, rootInjection, ref transientId, out _);
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

                            var hasCycle = path.Any(i => i.Current.Node.Binding == depNode.Binding);
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

                            var depVariable = GetVariable(currentStatement, currentBlock, map, blockMap, depNode, depInjection, ref transientId, out var isNew);
                            isAlreadyCreated |= !isNew;
                            var isBlockStatement = !variable.Node.IsEnumerable() && !variable.Node.IsAsyncEnumerable() && !variable.Node.IsFactory();
                            var isBlock = depNode.Lifetime is not Lifetime.Transient and not Lifetime.PerBlock || variable.Node.IsLazy();
                            if (isBlock)
                            {
                                var depBlock = new Block(blockId++, currentStatement, new LinkedList<IStatement>());
                                depBlock.Statements.AddFirst(depVariable);
                                if (!isAlreadyCreated)
                                {
                                    blocks.Push(depBlock);
                                }

                                if (isBlockStatement)
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

                                if (isBlockStatement)
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
        IStatement? parent,
        Block block,
        IDictionary<MdBinding, Variable> map,
        IDictionary<int, Variable> blockMap,
        in DependencyNode node,
        in Injection injection,
        ref int transientId,
        out bool isNew)
    {
        if (!node.IsArg())
        {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (node.Lifetime)
            {
                case Lifetime.Transient:
                    isNew = true;
                    return new Variable(parent, transientId++, node, injection, new List<IStatement>(), new VariableInfo(), node.IsLazy());
                
                case Lifetime.PerBlock:
                {
                    if (blockMap.TryGetValue(block.Id, out var blockVariable))
                    {
                        isNew = false;
                        return blockVariable;
                    }
                
                    blockVariable = new Variable(parent, transientId++, node, injection, new List<IStatement>(), new VariableInfo(), node.IsLazy());
                    blockMap.Add(block.Id, blockVariable);
                    isNew = true;
                    return blockVariable;
                }
            }
        }

        if (map.TryGetValue(node.Binding, out var variable))
        {
            variable.Info.RefCount++;
            isNew = true;
            return variable with
            {
                Parent = parent,
                Injection = injection,
                Args = new List<IStatement>()
            };
        }

        variable = new Variable(parent, node.Binding.Id, node, injection, new List<IStatement>(), new VariableInfo(), node.IsLazy());
        map.Add(node.Binding, variable);
        isNew = true;
        return variable;
    }
}