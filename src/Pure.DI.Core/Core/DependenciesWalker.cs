// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable MemberCanBeProtected.Global
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
        
        if (node.Construct is { } construction)
        {
            VisitConstruct(construction);
        }
    }

    public virtual void VisitRoot(in DpRoot root)
    {
        VisitInjection(root.Injection, ImmutableArray.Create(root.Source.Source.GetLocation()));
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
            VisitInjection(injection, ImmutableArray.Create(factory.Source.Source.GetLocation()));
        }
    }

    public virtual void VisitArg(in DpArg arg)
    {
    }

    public virtual void VisitConstruct(DpConstruct construct)
    {
        foreach (var injection in construct.Injections)
        {
            VisitInjection(injection, ImmutableArray.Create(construct.Binding.Source.GetLocation()));
        }
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
        VisitInjection(property.Injection, property.Property.Locations);
    }

    public virtual void VisitField(in DpField field)
    {
        VisitInjection(field.Injection, field.Field.Locations);
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
        VisitInjection(parameter.Injection, parameter.ParameterSymbol.Locations);
    }

    public virtual void VisitInjection(in Injection injection, in ImmutableArray<Location> locations)
    {
    }
}