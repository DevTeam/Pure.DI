// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

sealed class InstanceDpProvider(
    ISemantic semantic,
    IAttributes attributes,
    IWildcardMatcher wildcardMatcher,
    IRegistryManager<MdInjectionSite> registryManager,
    IInjectionSiteFactory injectionSiteFactory,
    ILocationProvider locationProvider) : IInstanceDpProvider
{
    public InstanceDp Get(
        MdSetup setup,
        ITypeConstructor typeConstructor,
        INamedTypeSymbol implementationType)
    {
        var actualImplementationType = typeConstructor.Construct(setup, implementationType);
        var setupAttributesBuilder = ImmutableArray.CreateBuilder<IMdAttribute>(
            setup.OrdinalAttributes.Length
            + setup.TagAttributes.Length
            + setup.TypeAttributes.Length);
        setupAttributesBuilder.AddRange(setup.OrdinalAttributes);
        setupAttributesBuilder.AddRange(setup.TagAttributes);
        setupAttributesBuilder.AddRange(setup.TypeAttributes);
        var setupAttributes = setupAttributesBuilder.MoveToImmutable();
        var methods = new List<DpMethod>();
        var fields = new List<DpField>();
        var properties = new List<DpProperty>();
        foreach (var member in GetMembers(actualImplementationType))
        {
            if (!semantic.IsAccessible(member))
            {
                continue;
            }

            switch (member)
            {
                case IMethodSymbol method:
                    if (method.MethodKind == MethodKind.Ordinary
                        && GetOrdinal(setup, setupAttributes, member, method) is {} methodOrdinal)
                    {
                        methods.Add(new DpMethod(method, methodOrdinal, GetParameters(setup, method.Parameters, typeConstructor), locationProvider));
                    }

                    break;

                case IFieldSymbol field:
                    if (field is { IsReadOnly: false, IsStatic: false, IsConst: false }
                        && (field.IsRequired ? 0 : GetOrdinal(setup, setupAttributes, member)) is {} fieldOrdinal)
                    {
                        var type = field.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                        fields.Add(
                            new DpField(
                                field,
                                fieldOrdinal,
                                new Injection(
                                    InjectionKind.Field,
                                    RefKind.None,
                                    attributes.GetAttribute(setup.SemanticModel, setup.TypeAttributes, field, AttributeKind.Type, typeConstructor.Construct(setup, type)),
                                    GetTagAttribute(setup, field),
                                    field.Locations)));
                    }

                    break;

                case IPropertySymbol property:
                    if (property is { IsReadOnly: false, IsStatic: false, IsIndexer: false }
                        && (property.IsRequired ? 0 : GetOrdinal(setup, setupAttributes, member)) is {} propertyOrdinal)
                    {
                        var type = property.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                        properties.Add(
                            new DpProperty(
                                property,
                                propertyOrdinal,
                                new Injection(
                                    InjectionKind.Property,
                                    RefKind.None,
                                    attributes.GetAttribute(setup.SemanticModel, setup.TypeAttributes, property, AttributeKind.Type, typeConstructor.Construct(setup, type)),
                                    GetTagAttribute(setup, property),
                                    property.Locations)));
                    }

                    break;
            }
        }

        return new InstanceDp(
            methods.ToImmutableArray(),
            fields.ToImmutableArray(),
            properties.ToImmutableArray());
    }

    private int? GetOrdinal(MdSetup setup, ImmutableArray<IMdAttribute> setupAttributes, ISymbol member, IMethodSymbol method) =>
        GetOrdinal(setup, setupAttributes, member) ??
        (method.Parameters.Length > 0  ? method.Parameters.Select(i => GetOrdinal(setup, setupAttributes, i)).Max() : null);

    private int? GetOrdinal(MdSetup setup, ImmutableArray<IMdAttribute> setupAttributes, ISymbol member) =>
        attributes.GetAttribute(setup.SemanticModel, setupAttributes, member, AttributeKind.Ordinal, default(int?))
        ?? (attributes.GetAttribute(setup.SemanticModel, setupAttributes, member, AttributeKind.Type, default(ITypeSymbol?)) is not null
            || attributes.GetAttribute(setup.SemanticModel, setupAttributes, member, AttributeKind.Tag, default(object?))
                is not null ? 0
            : null);

    public ImmutableArray<DpParameter> GetParameters(
        in MdSetup setup,
        in ImmutableArray<IParameterSymbol> parameters,
        ITypeConstructor typeConstructor)
    {
        if (parameters.Length == 0)
        {
            return ImmutableArray<DpParameter>.Empty;
        }

        var dependenciesBuilder = ImmutableArray.CreateBuilder<DpParameter>(parameters.Length);
        foreach (var parameter in parameters)
        {
            var type = parameter.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            dependenciesBuilder.Add(
                new DpParameter(
                    parameter,
                    new Injection(
                        InjectionKind.Parameter,
                        parameter.RefKind,
                        attributes.GetAttribute(setup.SemanticModel, setup.TypeAttributes, parameter, AttributeKind.Type, typeConstructor.Construct(setup, type)),
                        GetTagAttribute(setup, parameter),
                        parameter.Locations)));
        }

        return dependenciesBuilder.MoveToImmutable();
    }

    private static IEnumerable<ISymbol> GetMembers(ITypeSymbol type)
    {
        var members = new List<ISymbol>();
        while (true)
        {
            members.AddRange(type.GetMembers());
            if (type.BaseType is {} baseType)
            {
                type = baseType;
                continue;
            }

            break;
        }

        return members;
    }

    private object? GetTagAttribute(
        MdSetup setup,
        ISymbol member) =>
        attributes.GetAttribute(setup.SemanticModel, setup.TagAttributes, member, AttributeKind.Tag, default(object?))
        ?? TryCreateTagOnSite(setup, member);

    private object? TryCreateTagOnSite(
        MdSetup setup,
        ISymbol symbol)
    {
        if (setup.TagOn.Count == 0)
        {
            return null;
        }

        var injectionSite = injectionSiteFactory.CreateInjectionSite(symbol.ContainingSymbol, symbol.Name);
        var injectionSiteSpan = injectionSite.AsSpan();
        foreach (var tagOnSite in setup.TagOn)
        {
            foreach (var site in tagOnSite.InjectionSites)
            {
                if (!wildcardMatcher.Match(site.Site.AsSpan(), injectionSiteSpan))
                {
                    continue;
                }

                registryManager.Register(setup, site);
                return tagOnSite;
            }
        }

        return null;
    }
}