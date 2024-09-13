// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedParameter.Global

namespace Pure.DI.Core;

internal class DependenciesWalker<TContext>
{
    public virtual void VisitDependencyNode(in TContext ctx, DependencyNode node)
    {
        if (node.Root is { } root)
        {
            VisitRoot(ctx, root);
        }

        if (node.Implementation is { } implementation)
        {
            VisitImplementation(ctx, implementation);
        }

        if (node.Factory is { } factory)
        {
            VisitFactory(ctx, factory);
        }

        if (node.Arg is { } arg)
        {
            VisitArg(ctx, arg);
        }

        if (node.Construct is { } construction)
        {
            VisitConstruct(ctx, construction);
        }
    }

    public virtual void VisitRoot(in TContext ctx, in DpRoot root)
    {
        VisitInjection(ctx, root.Injection, false, default, ImmutableArray.Create(root.Source.Source.GetLocation()));
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
        foreach (var injection in factory.Injections)
        {
            VisitInjection(ctx, injection, false, default, ImmutableArray.Create(factory.Source.Source.GetLocation()));
        }
    }

    public virtual void VisitArg(in TContext ctx, in DpArg arg)
    {
    }

    public virtual void VisitConstruct(in TContext ctx, DpConstruct construct)
    {
        foreach (var injection in construct.Injections)
        {
            VisitInjection(ctx, injection, false, default, ImmutableArray.Create(construct.Binding.Source.GetLocation()));
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
        if (property.Property.SetMethod is not { } setMethod)
        {
            return;
        }

        if (setMethod.Parameters is not [{ } parameter])
        {
            return;
        }

        VisitInjection(
            ctx,
            property.Injection,
            parameter.HasExplicitDefaultValue,
            parameter.HasExplicitDefaultValue ? parameter.ExplicitDefaultValue : default,
            property.Property.Locations);
    }

    public virtual void VisitField(in TContext ctx, in DpField field)
    {
        VisitInjection(
            ctx,
            field.Injection,
            field.Field.HasConstantValue,
            field.Field.HasConstantValue ? field.Field.ConstantValue : default,
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
            parameter.ParameterSymbol.HasExplicitDefaultValue ? parameter.ParameterSymbol.ExplicitDefaultValue : default,
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
}