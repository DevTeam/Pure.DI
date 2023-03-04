// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
namespace Pure.DI.Core;

internal class DependenciesWalker
{
    public virtual void VisitDependencyNode(DependencyNode node)
    {
        if (node.Root is { } root)
        {
            VisitRoot(root);
        }
        
        if (node.Implementation is { } implementation)
        {
            VisitImplementation(implementation);
        }
                
        if (node.Factory is { } factory)
        {
            VisitFactory(factory);
        }
        
        if (node.Arg is { } arg)
        {
            VisitArg(arg);
        }
    }

    public virtual void VisitRoot(in DpRoot root)
    {
        VisitInjection(root.Injection);
    }

    public virtual void VisitImplementation(in DpImplementation implementation)
    {
        if (implementation.Constructor is var constructor)
        {
            VisitConstructor(constructor);
        }
        
        foreach (var field in implementation.Fields)
        {
            VisitField(field);
        }

        foreach (var property in implementation.Properties)
        {
            VisitProperty(property);
        }

        foreach (var method in implementation.Methods)
        {
            VisitMethod(method);
        }
    }
    
    public virtual void VisitFactory(in DpFactory factory)
    {
        foreach (var injection in factory.Injections)
        {
            VisitInjection(injection);
        }
    }
    
    public virtual void VisitArg(in DpArg arg)
    {
    }

    public virtual void VisitMethod(in DpMethod method)
    {
        foreach (var parameter in method.Parameters) 
        {
            VisitParameter(parameter);
        }
    }

    public virtual void VisitProperty(in DpProperty property)
    {
        VisitInjection(property.Injection);
    }

    public virtual void VisitField(in DpField field)
    {
        VisitInjection(field.Injection);
    }

    public virtual void VisitConstructor(in DpMethod constructor)
    {
        foreach (var parameter in constructor.Parameters)
        {
            VisitParameter(parameter);
        }
    }

    public virtual void VisitParameter(in DpParameter parameter)
    {
        VisitInjection(parameter.Injection);
    }

    public virtual void VisitInjection(in Injection injection)
    {
    }
}