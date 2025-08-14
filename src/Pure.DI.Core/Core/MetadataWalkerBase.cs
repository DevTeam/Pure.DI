// ReSharper disable MemberCanBeProtected.Global

// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable UnusedParameter.Global
namespace Pure.DI.Core;

abstract class MetadataWalkerBase : IMetadataVisitor
{
    public virtual void VisitSetup(in MdSetup setup)
    {
        foreach (var md in setup.Bindings)
        {
            VisitBinding(md);
        }

        foreach (var md in setup.Roots)
        {
            VisitRoot(md);
        }

        foreach (var md in setup.DependsOn)
        {
            VisitDependsOn(md);
        }

        foreach (var md in setup.TypeAttributes)
        {
            VisitTypeAttribute(md);
        }

        foreach (var md in setup.TagAttributes)
        {
            VisitTagAttribute(md);
        }

        foreach (var md in setup.OrdinalAttributes)
        {
            VisitOrdinalAttribute(md);
        }
    }

    public virtual void VisitUsingDirectives(in MdUsingDirectives usingDirectives)
    {
    }

    public virtual void VisitBinding(in MdBinding binding)
    {
        foreach (var md in binding.Contracts)
        {
            VisitContract(md);
        }

        if (binding.Lifetime is {} lifetime)
        {
            VisitLifetime(lifetime);
        }

        foreach (var tag in binding.Tags)
        {
            VisitTag(tag);
        }

        if (binding.Implementation is {} implementation)
        {
            VisitImplementation(implementation);
        }

        if (binding.Factory is {} factory)
        {
            VisitFactory(factory);
        }

        if (binding.Arg is {} arg)
        {
            VisitArg(arg);
        }

        if (binding.Construct is {} construction)
        {
            VisitConstruction(construction);
        }
    }

    public virtual void VisitContract(in MdContract contract)
    {
        foreach (var tag in contract.Tags)
        {
            VisitTag(tag);
        }
    }

    public virtual void VisitImplementation(in MdImplementation implementation)
    {
    }

    public virtual void VisitFactory(in MdFactory factory)
    {
        foreach (var md in factory.Resolvers)
        {
            VisitResolver(md);
        }

        foreach (var md in factory.Initializers)
        {
            VisitInitializer(md);
        }
    }

    public virtual void VisitInitializer(MdInitializer md)
    {
    }

    public virtual void VisitRoot(in MdRoot root)
    {
        if (root.Tag is {} tag)
        {
            VisitTag(tag);
        }
    }

    public virtual void VisitGenericTypeArgument(in MdGenericTypeArgument genericTypeArgument)
    {
    }

    public virtual void VisitGenericTypeArgumentAttribute(in MdGenericTypeArgumentAttribute genericTypeArgumentAttribute)
    {
    }

    public virtual void VisitResolver(in MdResolver resolver)
    {
        if (resolver.Tag is {} tag)
        {
            VisitTag(tag);
        }
    }

    public virtual void VisitArg(in MdArg arg)
    {
    }

    public virtual void VisitDefaultLifetime(in MdDefaultLifetime defaultLifetime)
    {
    }

    public virtual void VisitDependsOn(in MdDependsOn dependsOn)
    {
    }

    public virtual void VisitTypeAttribute(in MdTypeAttribute typeAttribute)
    {
    }

    public virtual void VisitTagAttribute(in MdTagAttribute tagAttribute)
    {
    }

    public virtual void VisitOrdinalAttribute(in MdOrdinalAttribute ordinalAttribute)
    {
    }

    public virtual void VisitLifetime(in MdLifetime lifetime)
    {
    }

    public virtual void VisitTag(in MdTag tag)
    {
    }

    public virtual void VisitAccumulator(in MdAccumulator accumulator)
    {
    }

    public virtual void VisitHint(in MdHint hint)
    {
    }

    public virtual void VisitFinish()
    {
    }

    public virtual void VisitConstruction(in MdConstruct construct)
    {
    }
}