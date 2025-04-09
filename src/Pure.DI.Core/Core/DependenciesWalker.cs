// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedParameter.Global

namespace Pure.DI.Core;

class DependenciesWalker<TContext>
{
    public virtual void VisitDependencyNode(in TContext ctx, DependencyNode node)
    {
        if (node.Root is {} root)
        {
            VisitRoot(ctx, root);
            return;
        }

        if (node.Implementation is {} implementation)
        {
            VisitImplementation(ctx, implementation);
            return;
        }

        if (node.Factory is {} factory)
        {
            VisitFactory(ctx, factory);
            return;
        }

        if (node.Arg is {} arg)
        {
            VisitArg(ctx, arg);
            return;
        }

        if (node.Construct is {} construction)
        {
            VisitConstruct(ctx, construction);
        }
    }

    public virtual void VisitRoot(in TContext ctx, in DpRoot root)
    {
        VisitInjection(ctx, root.Injection, false, null, ImmutableArray.Create(root.Source.Source.GetLocation()));
    }

    public virtual void VisitImplementation(in TContext ctx, in DpImplementation implementation)
    {
        if (implementation.Constructor is var constructor)
        {
            VisitConstructor(ctx, constructor);
        }

        foreach (var field in implementation.Fields)
        {
            VisitField(ctx, field);
        }

        foreach (var property in implementation.Properties)
        {
            VisitProperty(ctx, property);
        }

        foreach (var method in implementation.Methods)
        {
            VisitMethod(ctx, method);
        }
    }

    public virtual void VisitFactory(in TContext ctx, in DpFactory factory)
    {
        foreach (var resolver in factory.Resolvers)
        {
            VisitResolver(ctx, resolver);
        }

        foreach (var initializer in factory.Initializers)
        {
            VisitInitializer(ctx, initializer);
        }
    }

    public virtual void VisitArg(in TContext ctx, in DpArg arg)
    {
    }

    public virtual void VisitConstruct(in TContext ctx, DpConstruct construct)
    {
        foreach (var injection in construct.Injections)
        {
            VisitInjection(ctx, injection, false, null, ImmutableArray.Create(construct.Binding.Source.GetLocation()));
        }
    }

    public virtual void VisitMethod(in TContext ctx, in DpMethod method)
    {
        foreach (var parameter in method.Parameters)
        {
            VisitParameter(ctx, parameter);
        }
    }

    public virtual void VisitProperty(in TContext ctx, in DpProperty property)
    {
        if (property.Property.SetMethod is not {} setMethod)
        {
            return;
        }

        if (setMethod.Parameters is not [{} parameter])
        {
            return;
        }

        VisitInjection(
            ctx,
            property.Injection,
            parameter.HasExplicitDefaultValue,
            parameter.HasExplicitDefaultValue ? parameter.ExplicitDefaultValue : null,
            property.Property.Locations);
    }

    public virtual void VisitField(in TContext ctx, in DpField field)
    {
        VisitInjection(
            ctx,
            field.Injection,
            field.Field.HasConstantValue,
            field.Field.HasConstantValue ? field.Field.ConstantValue : null,
            field.Field.Locations);
    }

    public virtual void VisitConstructor(in TContext ctx, in DpMethod constructor)
    {
        foreach (var parameter in constructor.Parameters)
        {
            VisitParameter(ctx, parameter);
        }
    }

    public virtual void VisitParameter(in TContext ctx, in DpParameter parameter)
    {
        VisitInjection(
            ctx,
            parameter.Injection,
            parameter.ParameterSymbol.HasExplicitDefaultValue,
            parameter.ParameterSymbol.HasExplicitDefaultValue ? parameter.ParameterSymbol.ExplicitDefaultValue : null,
            parameter.ParameterSymbol.Locations);
    }

    public virtual void VisitInjection(
        in TContext ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations)
    {
    }

    public virtual void VisitResolver(in TContext ctx, DpResolver resolver)
    {
        VisitInjection(ctx, resolver.Injection, false, null, ImmutableArray.Create(resolver.Source.Source.GetLocation()));
    }

    public virtual void VisitInitializer(in TContext ctx, DpInitializer initializer)
    {
        foreach (var field in initializer.Fields)
        {
            VisitField(ctx, field);
        }

        foreach (var property in initializer.Properties)
        {
            VisitProperty(ctx, property);
        }

        foreach (var method in initializer.Methods)
        {
            VisitMethod(ctx, method);
        }
    }
}