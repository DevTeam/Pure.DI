// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class VariablesBuilder : IVariablesBuilder
{
    private readonly Dictionary<MdBinding, int> _bindings = new();
    private int _id;
    
    public Block Build(
        IGraph<DependencyNode, Dependency> graph,
        IDictionary<MdBinding, Variable> map,
        DependencyNode rootNode,
        Injection rootInjection)
    {
        var rootBlock = new Block(default, new LinkedList<IStatement>());
        var rootVar = CreateVariable(rootBlock, map, rootNode, rootInjection);
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

                                isAlreadyCreated = depNode.IsLazy() && path
                                    .Select(i => i.Current.Node)
                                    .GroupBy(i => i.Binding)
                                    .Any(i => i.Count() > 1);
                            }

                            var depVariable = CreateVariable(currentStatement, map, depNode, depInjection);
                            if (isAlreadyCreated)
                            {
                                depVariable.Info.IsAlreadyCreated = true;
                                depVariable.Info.RefCount--;
                            }
                            
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
    
    private Variable CreateVariable(
        IStatement? parent,
        IDictionary<MdBinding, Variable> map,
        in DependencyNode node,
        in Injection injection)
    {
        var isSharedVar = node.IsArg() || node.Lifetime != Lifetime.Transient;
        if (isSharedVar)
        {
            if (map.TryGetValue(node.Binding, out var currentVar))
            {
                currentVar.Info.RefCount++;
                return currentVar with
                {
                    Parent = parent,
                    Injection = injection,
                    Args = new List<IStatement>()
                };
            }
        }

        var id = GetId(node);
        var var = new Variable(parent, id, node, injection, new List<IStatement>(), new VariableInfo());
        var.Info.RefCount++;
        if (isSharedVar)
        {
            map.Add(node.Binding, var);
        }

        return var;
    }

    private int GetId(in DependencyNode node)
    {
        if (node.Lifetime == Lifetime.Transient)
        {
            return _id++;
        }

        if (_bindings.TryGetValue(node.Binding, out var id))
        {
            return id;
        }
        
        id = _id++;
        _bindings.Add(node.Binding, id);
        return id;
    }
}