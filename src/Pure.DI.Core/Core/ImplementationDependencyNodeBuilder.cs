// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace Pure.DI.Core;

internal sealed class ImplementationDependencyNodeBuilder(
    ILogger<ImplementationDependencyNodeBuilder> logger,
    IBuilder<DpImplementation, IEnumerable<DpImplementation>> implementationVariantsBuilder)
    :
        IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(MdSetup setup)
    {
        foreach (var binding in setup.Bindings)
        {
            if (binding.Implementation is not { } implementation)
            {
                continue;
            }

            if (implementation.Type is not INamedTypeSymbol implementationType)
            {
                yield break;
            }

            if (implementationType.IsAbstract)
            {
                throw new CompileErrorException($"The {implementationType} cannot be constructed due to being an abstract type.", implementation.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            }

            var compilation = binding.SemanticModel.Compilation;
            var constructors = new List<DpMethod>();
            var hasExplicitlyDeclaredNonStaticCtor = implementationType.Constructors.Any(i => !i.IsImplicitlyDeclared && !i.IsStatic);
            foreach (var constructor in implementationType.Constructors)
            {
                if (hasExplicitlyDeclaredNonStaticCtor && constructor.IsImplicitlyDeclared)
                {
                    continue;
                }
                
                if (constructor.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.Public or Accessibility.Friend))
                {
                    continue;
                }

                constructors.Add(
                    new DpMethod(
                        constructor,
                        GetAttribute(setup.OrdinalAttributes, constructor, default(int?)),
                        GetParameters(setup, constructor.Parameters, compilation, setup.TypeConstructor)));
            }

            if (!constructors.Any())
            {
                throw new CompileErrorException($"The instance of {implementationType} cannot be instantiated due to no accessible constructor available.", implementation.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            }

            var methods = new List<DpMethod>();
            var fields = new List<DpField>();
            var properties = new List<DpProperty>();
            foreach (var member in implementationType.GetMembers())
            {
                if (member.IsStatic || member.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.Public or Accessibility.Friend))
                {
                    continue;
                }

                switch (member)
                {
                    case IMethodSymbol method:
                        if (method.MethodKind == MethodKind.Ordinary)
                        {
                            var ordinal = GetAttribute(setup.OrdinalAttributes, member, default(int?));
                            if (ordinal.HasValue)
                            {
                                methods.Add(new DpMethod(method, ordinal.Value, GetParameters(setup, method.Parameters, compilation, setup.TypeConstructor)));
                            }
                        }

                        break;

                    case IFieldSymbol field:
                        if (field is { IsReadOnly: false, IsStatic: false, IsConst: false })
                        {
                            var type = field.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                            var ordinal = GetAttribute(setup.OrdinalAttributes, member, default(int?));
                            if (ordinal.HasValue || field.IsRequired)
                            {
                                fields.Add(
                                    new DpField(
                                        field,
                                        ordinal,
                                        new Injection(
                                            GetAttribute(setup.TypeAttributes, field, setup.TypeConstructor?.Construct(compilation, type) ?? type),
                                            GetAttribute(setup.TagAttributes, field, default(object?)))));
                            }
                        }

                        break;

                    case IPropertySymbol property:
                        if (property is { IsReadOnly: false, IsStatic: false, IsIndexer: false })
                        {
                            var type = property.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                            var ordinal = GetAttribute(setup.OrdinalAttributes, member, default(int?));
                            if (ordinal.HasValue || property.IsRequired)
                            {
                                properties.Add(
                                    new DpProperty(
                                        property,
                                        ordinal,
                                        new Injection(
                                            GetAttribute(setup.TypeAttributes, property, setup.TypeConstructor?.Construct(compilation, type) ?? type),
                                            GetAttribute(setup.TagAttributes, property, default(object?)))));
                            }
                        }

                        break;
                }
            }

            var baseImplementations = constructors
                .Select(constructor => 
                    new DpImplementation(
                        implementation,
                        binding,
                        constructor,
                        methods.ToImmutableArray(),
                        properties.ToImmutableArray(),
                        fields.ToImmutableArray()))
                .ToArray();

            var implementationsWithOrdinal = baseImplementations.Where(i => i.Constructor.Ordinal.HasValue).ToArray();
            if (implementationsWithOrdinal.Any())
            {
                var variantsWithOrdinal = implementationsWithOrdinal
                    .OrderBy(i => i.Constructor.Ordinal)
                    .SelectMany(impl => 
                        implementationVariantsBuilder.Build(impl)
                            .OrderByDescending(i => GetInjectionsCount(i)));

                foreach (var node in CreateNodes(variantsWithOrdinal))
                {
                    yield return node;
                }
                
                continue;
            }

            var implementations = baseImplementations
                .SelectMany(implementationVariantsBuilder.Build)
                .Select(impl => (Implementation: impl, InjectionsCount: GetInjectionsCount(impl)))
                .ToArray();

            var maxInjectionsCount = implementations.Max(i => i.InjectionsCount);
            
            var orderedImplementations = implementations
                .OrderBy(i => maxInjectionsCount - i.InjectionsCount)
                .ThenByDescending(i => i.Implementation.Constructor.Method.DeclaredAccessibility)
                .Select(i => i.Implementation);

            foreach (var node in CreateNodes(orderedImplementations))
            {
                yield return node;
            }
        }
    }

    private static IEnumerable<DependencyNode> CreateNodes(IEnumerable<DpImplementation> implementations) => 
        implementations.Select((implementation, variantId) => new DependencyNode(variantId, implementation.Binding, Implementation: implementation));

    private static int GetInjectionsCount(in DpImplementation implementation)
    {
        var injectionsWalker = new DependenciesToInjectionsCountWalker();
        injectionsWalker.VisitImplementation(Unit.Shared, implementation);
        return injectionsWalker.Count;
    }

    private ImmutableArray<DpParameter> GetParameters(
        in MdSetup setup,
        in ImmutableArray<IParameterSymbol> parameters,
        Compilation compilation,
        ITypeConstructor? typeConstructor)
    {
        var dependenciesBuilder = ImmutableArray.CreateBuilder<DpParameter>(parameters.Length);
        foreach (var parameter in parameters)
        {
            var type = parameter.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            dependenciesBuilder.Add(
                new DpParameter(
                    parameter,
                    new Injection(
                        GetAttribute(setup.TypeAttributes, parameter, typeConstructor?.Construct(compilation, type) ?? type),
                        GetAttribute(setup.TagAttributes, parameter, default(object?)))));
        }

        return dependenciesBuilder.MoveToImmutable();
    }
    
    private T GetAttribute<TMdAttribute, T>(
        in ImmutableArray<TMdAttribute> attributeMetadata,
        ISymbol member,
        T defaultValue)
        where TMdAttribute: IMdAttribute
    {
        foreach (var attribute in attributeMetadata)
        {
            var attributeData = member.GetAttributes(attribute.AttributeType);
            switch (attributeData.Count)
            {
                case 1:
                    var args = attributeData[0].ConstructorArguments;
                    if (attribute.ArgumentPosition > args.Length)
                    {
                        logger.CompileError($"The argument position {attribute.ArgumentPosition.ToString()} of attribute {attribute.Source} is out of range [0..{args.Length.ToString()}].", attribute.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                    }

                    var typedConstant = args[attribute.ArgumentPosition];
                    if (typedConstant.Value is T value)
                    {
                        return value;
                    }

                    break;

                case > 1:
                    throw new CompileErrorException($"{member} of the type {member.ContainingType} cannot be processed because it is marked with multiple mutually exclusive attributes.", attribute.Source.GetLocation(), LogId.ErrorInvalidMetadata);                 
            }
        }

        return defaultValue;
    }

    private sealed class DependenciesToInjectionsCountWalker: DependenciesWalker<Unit>
    {
        private int _count;

        public int Count => _count;

        public override void VisitInjection(
            in Unit ctx,
            in Injection injection,
            bool hasExplicitDefaultValue,
            object? explicitDefaultValue,
            in ImmutableArray<Location> locations) => _count++;
    }
}