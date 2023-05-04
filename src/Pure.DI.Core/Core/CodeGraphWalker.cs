// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable InvertIf
// ReSharper disable UnusedParameter.Global
// ReSharper disable VirtualMemberNeverOverridden.Global
namespace Pure.DI.Core;

internal class CodeGraphWalker<TContext>
{
    private readonly IVarIdGenerator _idGenerator;

    protected CodeGraphWalker(IVarIdGenerator idGenerator) => _idGenerator = idGenerator;

    protected void VisitGraph(
        TContext context,
        DependencyGraph dependencyGraph, 
        IDictionary<MdBinding, Variable> variables,
        CancellationToken cancellationToken)
    {
        foreach (var root in dependencyGraph.Roots.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            VisitRoot(context, dependencyGraph, variables, root, cancellationToken);
        }
    }

    protected virtual void VisitRoot(
        TContext context,
        DependencyGraph dependencyGraph,
        IDictionary<MdBinding, Variable> variables,
        Root root,
        CancellationToken cancellationToken)
    {
        var rootVariable = CreateVariable(dependencyGraph, variables, root.Node, root.Injection);
        VisitRootVariable(context, dependencyGraph, variables, rootVariable, cancellationToken);
        
        var keysToRemove = variables
            .Where(i => i.Value.Node.Lifetime != Lifetime.Singleton && i.Value.Node.Arg is null)
            .Select(i => i.Key)
            .ToArray();

        foreach (var binding in keysToRemove)
        {
            variables.Remove(binding);
        }
        
        foreach (var variable in variables.Values)
        {
            variable.IsCreated = false;
        }
    }

    protected void VisitRootVariable(
        TContext context,
        DependencyGraph dependencyGraph,
        IDictionary<MdBinding, Variable> variables,
        Variable rootVariable,
        CancellationToken cancellationToken)
    {
        var targets = new Stack<Variable>();
        var blocks = new Stack<Block>();
        var blockRootVariables = new Stack<Variable>();
        blockRootVariables.Push(rootVariable);
        while (blockRootVariables.TryPop(out var target))
        {
            cancellationToken.ThrowIfCancellationRequested();
            targets.Push(target);
            var instantiations = new Stack<Instantiation>();
            while (targets.TryPop(out var targetVariable))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var arguments = new List<Variable>();
                if (dependencyGraph.Graph.TryGetInEdges(targetVariable.Node, out var dependencies))
                {
                    foreach (var dependency in dependencies)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        ProcessVariable(CreateVariable(dependencyGraph, variables, dependency.Source, dependency.Injection));
                    }
                }

                instantiations.Push(new Instantiation(targetVariable, arguments.ToImmutableArray()));
                
                void ProcessVariable(Variable var)
                {
                    var isBlockRoot = var.IsBlockRoot;
                    if (!isBlockRoot && var.Node.Lifetime == Lifetime.PerResolve)
                    {
                        if (dependencyGraph.Graph.TryGetOutEdges(var.Node, out var outDependencies))
                        {
                            isBlockRoot = outDependencies.Select(i => i.Target.Factory is not null ? 2 : 1).Sum() > 1;
                        }
                    }
                        
                    if (isBlockRoot)
                    {
                        blockRootVariables.Push(var);
                    }
                    else
                    {
                        if (targetVariable.Node.Factory is null)
                        {
                            targets.Push(var);
                        }
                    }

                    if (targetVariable.Node.Construct is { Source.Kind: MdConstructKind.Enumerable })
                    {
                        // Will be created in a Func
                        var.IsCreated = true;
                    }
                        
                    arguments.Add(var);   
                }
            }

            blocks.Push(new Block(target, instantiations.ToImmutableArray()));
        }

        while (blocks.TryPop(out var block))
        {
            VisitBlock(context, dependencyGraph, rootVariable, block, cancellationToken);
        }
    }

    protected virtual void VisitBlock(TContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Block block,
        CancellationToken cancellationToken)
    {
        foreach (var instantiation in block.Instantiations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            VisitInstantiation(context, dependencyGraph, root, instantiation, cancellationToken);
        }
    }

    protected virtual void VisitInstantiation(
        TContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Instantiation instantiation,
        CancellationToken cancellationToken)
    {
        if (instantiation.Target.Node.Implementation is { } implementation)
        {
            VisitImplementation(context, root, instantiation, implementation, cancellationToken);
        }
        else
        {
            if (instantiation.Target.Node.Factory is { } factory)
            {
                VisitFactory(context, dependencyGraph, root, instantiation, factory, cancellationToken);
            }
            else
            {
                if (instantiation.Target.Node.Arg is { } arg)
                {
                    VisitArg(context, root, instantiation, arg);
                }
                else
                {
                    if (instantiation.Target.Node.Construct is { } construct)
                    {
                        switch (construct.Source.Kind)
                        {
                            case MdConstructKind.Enumerable:
                                VisitEnumerableConstruct(context, dependencyGraph, root, construct, instantiation, cancellationToken);
                                break;
                
                            case MdConstructKind.Array:
                                VisitArrayConstruct(context, root, construct, instantiation);
                                break;
                            
                            case MdConstructKind.Span:
                                VisitSpanConstruct(context, root, construct, instantiation);
                                break;
                            
                            case MdConstructKind.Composition:
                                VisitCompositionConstruct(context, root, construct, instantiation);
                                break;
                            
                            case MdConstructKind.OnCannotResolve:
                                VisitOnCannotResolve(context, root, construct, instantiation);
                                break;
                        }
                    }
                }
            }
        }
    }

    protected virtual void VisitImplementation(
        TContext context,
        Variable root,
        Instantiation instantiation,
        in DpImplementation implementation,
        CancellationToken cancellationToken)
    {
        if (instantiation.Target.IsCreated)
        {
            return;
        }
        
        var args = instantiation.Arguments.ToList();
        var argsWalker = new DependenciesToVariablesWalker(args);
        argsWalker.VisitConstructor(implementation.Constructor);
        var ctorArgs = argsWalker.GetResult();

        var initOnlyProperties = ImmutableArray.CreateBuilder<(Variable InitOnlyVariable, DpProperty InitOnlyProperty)>();
        foreach (var initOnlyProperty in implementation.Properties.Where(i => i.Property.SetMethod?.IsInitOnly == true).OrderBy(i => i.Ordinal ?? int.MaxValue))
        {
            argsWalker.VisitProperty(initOnlyProperty);
            initOnlyProperties.Add((argsWalker.GetResult().Single(), initOnlyProperty));
        }

        VisitConstructor(
            context,
            instantiation,
            implementation,
            implementation.Constructor,
            ctorArgs,
            initOnlyProperties.ToImmutableArray());

        var visits = new List<(Action Run, int? Ordinal)>();
        foreach (var field in implementation.Fields)
        {
            argsWalker.VisitField(field);
            var fieldVariable = argsWalker.GetResult().Single();
            void VisitFieldAction() => VisitField(context, instantiation, field, fieldVariable);
            visits.Add((VisitFieldAction, field.Ordinal));
        }
        
        foreach (var property in implementation.Properties.Where(i => i.Property.SetMethod?.IsInitOnly != true))
        {
            argsWalker.VisitProperty(property);
            var propertyVariable = argsWalker.GetResult().Single();
            void VisitFieldAction() => VisitProperty(context, instantiation, property, propertyVariable);
            visits.Add((VisitFieldAction, property.Ordinal));
        }
        
        foreach (var method in implementation.Methods)
        {
            argsWalker.VisitMethod(method);
            var methodArgs = argsWalker.GetResult();
            void VisitMethodAction() => VisitMethod(context, instantiation, method, methodArgs);
            visits.Add((VisitMethodAction, method.Ordinal));
        }

        foreach (var visit in visits.OrderBy(i => i.Ordinal ?? int.MaxValue))
        {
            cancellationToken.ThrowIfCancellationRequested();
            visit.Run();
        }
    }

    protected virtual void VisitConstructor(
        TContext context,
        Instantiation instantiation,
        in DpImplementation implementation,
        in DpMethod constructor,
        in ImmutableArray<Variable> constructorArguments,
        in ImmutableArray<(Variable InitOnlyVariable, DpProperty InitOnlyProperty)> initOnlyProperties)
    {
    }

    protected virtual void VisitField(
        TContext context,
        Instantiation instantiation,
        in DpField field,
        Variable fieldVariable)
    {
    }

    protected virtual void VisitProperty(
        TContext context,
        Instantiation instantiation,
        in DpProperty property,
        Variable propertyVariable)
    {
    }

    protected virtual void VisitMethod(
        TContext context,
        Instantiation instantiation,
        in DpMethod method,
        in ImmutableArray<Variable> methodArguments)
    {
    }

    protected virtual void VisitFactory(
        TContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        Instantiation instantiation,
        in DpFactory factory,
        CancellationToken cancellationToken)
    {
    }

    protected virtual void VisitArg(
        TContext context,
        Variable rootVariable,
        Instantiation instantiation,
        in DpArg dpArg)
    {
    }

    protected virtual void VisitEnumerableConstruct(
        TContext context,
        DependencyGraph dependencyGraph,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation,
        CancellationToken cancellationToken)
    {
    }

    protected virtual void VisitArrayConstruct(
        TContext context,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation)
    {
    }

    protected virtual void VisitSpanConstruct(
        TContext context,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation)
    {
    }
    
    protected virtual void VisitCompositionConstruct(
        TContext context,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation)
    {
    }

    protected virtual void VisitOnCannotResolve(
        TContext context,
        Variable root,
        in DpConstruct construct,
        Instantiation instantiation)
    {
    }
    
    protected Variable CreateVariable(
        DependencyGraph source,
        IDictionary<MdBinding, Variable> variables,
        DependencyNode node,
        Injection injection)
    {
        switch (node)
        {
            case { Arg: not null }:
                if (variables.TryGetValue(node.Binding, out var argVar))
                {
                    return argVar;
                }

                argVar = new Variable(source, 0, node, injection)
                {
                    IsDeclared = true,
                    IsCreated = true
                };
                
                variables.Add(node.Binding, argVar);
                return argVar;

            case { Lifetime: Lifetime.Singleton }:
                if (variables.TryGetValue(node.Binding, out var singletonVar))
                {
                    return singletonVar.CreateLinkedVariable(injection);
                }

                singletonVar = new Variable(source, 0, node, injection)
                {
                    IsDeclared = true,
                    IsBlockRoot = true
                };

                variables.Add(node.Binding, singletonVar);
                return singletonVar;

            case { Lifetime: Lifetime.PerResolve }:
                if (variables.TryGetValue(node.Binding, out var perResolveVar))
                {
                    return perResolveVar.CreateLinkedVariable(injection);
                }

                perResolveVar = new Variable(source, _idGenerator.NextId, node, injection);
                variables.Add(node.Binding, perResolveVar);
                return perResolveVar;

            default:
                var transientVar = new Variable(source, _idGenerator.NextId, node, injection);
                return transientVar;
        }
    }
}