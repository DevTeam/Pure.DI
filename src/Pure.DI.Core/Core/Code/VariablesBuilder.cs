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
        var rootBlock = new Block(default, new LinkedList<IStatement>());
        var transientId = 0;
        var rootVar = GetVariable(rootBlock, map, rootNode, rootInjection, ref transientId);
        rootBlock.Statements.AddFirst(rootVar);
        var blocks = new Stack<Block>();
        blocks.Push(rootBlock);
        while (blocks.TryPop(out var currentBlock))
        {
            var stack = new Stack<IStatement>(currentBlock.Statements);
            while (stack.TryPop(out var currentStatement))
            {
                var path = currentStatement.GetPath().ToArray();
                var hasLazy = path.Any(i => i.Current.Node.IsLazy());
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

                            var depVariable = GetVariable(currentStatement, map, depNode, depInjection, ref transientId);
                            var isBlockStatement = !variable.Node.IsEnumerable() && !variable.Node.IsFactory();
                            var isBlock = depNode.Lifetime != Lifetime.Transient || variable.Node.IsLazy();
                            if (isBlock)
                            {
                                var depBlock = new Block(currentStatement, new LinkedList<IStatement>());
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
        IDictionary<MdBinding, Variable> map,
        in DependencyNode node,
        in Injection injection,
        ref int transientId)
    {
        if (node.Lifetime == Lifetime.Transient && !node.IsArg())
        {
            return new Variable(parent, transientId++, node, injection, new List<IStatement>(), new VariableInfo());
        }

        if (map.TryGetValue(node.Binding, out var variable))
        {
            variable.Info.RefCount++;
            return variable with
            {
                Parent = parent,
                Injection = injection,
                Args = new List<IStatement>()
            };
        }

        variable = new Variable(parent, node.Binding.Id, node, injection, new List<IStatement>(), new VariableInfo());
        map.Add(node.Binding, variable);
        return variable;
    }
}