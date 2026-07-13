// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedParameter.Global

namespace Pure.DI.Core;

class DependenciesWalker<TContext>(
    ILocationProvider locationProvider)
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
        VisitInjection(ctx, root.Injection, false, null, ImmutableArray.Create(locationProvider.GetLocation(root.Source.Source)), null);
    }

    public virtual void VisitImplementation(in TContext ctx, in DpImplementation implementation)
    {
        if (implementation.Constructor is var constructor)
        {
            VisitConstructor(ctx, constructor);
        }

        foreach (var field in implementation.Fields)
        {
            VisitField(ctx, field, null);
        }

        foreach (var property in implementation.Properties)
        {
            VisitProperty(ctx, property, null);
        }

        foreach (var method in implementation.Methods)
        {
            VisitMethod(ctx, method, null);
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
            VisitInjection(ctx, injection, false, null, ImmutableArray.Create(locationProvider.GetLocation(construct.Binding.Source)), null);
        }
    }

    public virtual void VisitMethod(in TContext ctx, in DpMethod method, int? position)
    {
        foreach (var parameter in method.Parameters)
        {
            VisitParameter(ctx, parameter, position);
        }
    }

    public virtual void VisitProperty(in TContext ctx, in DpProperty property, int? position)
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
            property.Property.Locations,
            position);
    }

    public virtual void VisitField(in TContext ctx, in DpField field, int? position)
    {
        VisitInjection(
            ctx,
            field.Injection,
            field.Field.HasConstantValue,
            field.Field.HasConstantValue ? field.Field.ConstantValue : null,
            field.Field.Locations,
            position);
    }

    public virtual void VisitConstructor(in TContext ctx, in DpMethod constructor)
    {
        foreach (var parameter in constructor.Parameters)
        {
            VisitParameter(ctx, parameter, null);
        }
    }

    public virtual void VisitParameter(in TContext ctx, in DpParameter parameter, int? position)
    {
        var hasExplicitDefaultValue = parameter.ParameterSymbol.HasExplicitDefaultValue
                                      && parameter.ParameterSymbol is { IsOptional: true };

        object? explicitDefaultValue = null;
        if (hasExplicitDefaultValue)
        {
            var defaultValue = parameter.ParameterSymbol.ExplicitDefaultValue;
            explicitDefaultValue = defaultValue;

            // For enum types, ExplicitDefaultValue returns the underlying type (e.g., int)
            // We need to convert it back to the actual enum value
            if (parameter.ParameterSymbol.Type.TypeKind == TypeKind.Enum && defaultValue is not null && parameter.ParameterSymbol.Type is INamedTypeSymbol enumType)
            {
                // For enum types, we need to find the enum member with the matching value
                var enumMember = enumType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(m => m.HasConstantValue && Equals(m.ConstantValue, defaultValue));

                if (enumMember is not null)
                {
                    explicitDefaultValue = enumMember;
                }
            }
        }

        VisitInjection(
            ctx,
            parameter.Injection,
            hasExplicitDefaultValue,
            explicitDefaultValue,
            parameter.ParameterSymbol.Locations,
            position);
    }

    public virtual void VisitInjection(
        in TContext ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations,
        int? position)
    {
    }

    public virtual void VisitResolver(in TContext ctx, DpResolver resolver)
    {
        VisitInjection(
            ctx,
            resolver.Injection,
            false,
            null,
            ImmutableArray.Create(locationProvider.GetLocation(resolver.Source.Source)),
            resolver.Source.Position);
    }

    public virtual void VisitInitializer(in TContext ctx, DpInitializer initializer)
    {
        foreach (var field in initializer.Fields)
        {
            VisitField(ctx, field, initializer.Source.Position);
        }

        foreach (var property in initializer.Properties)
        {
            VisitProperty(ctx, property, initializer.Source.Position);
        }

        foreach (var method in initializer.Methods)
        {
            VisitMethod(ctx, method, initializer.Source.Position);
        }
    }
}
