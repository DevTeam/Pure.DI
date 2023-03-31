// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable InvertIf
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core;

internal class ImplementationDependencyNodeBuilder : 
    IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    private readonly ILogger<ImplementationDependencyNodeBuilder> _logger;

    public ImplementationDependencyNodeBuilder(ILogger<ImplementationDependencyNodeBuilder> logger) => _logger = logger;

    public IEnumerable<DependencyNode> Build(MdSetup setup, CancellationToken cancellationToken)
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
                _logger.CompileError($"The {implementationType} cannot be constructed due to being an abstract type.", implementation.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                throw HandledException.Shared;
            }

            var compilation = binding.SemanticModel.Compilation;
            var constructors = new List<DpMethod>();
            foreach (var constructor in implementationType.Constructors)
            {
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
                _logger.CompileError($"The instance of {implementationType} cannot be instantiated due to no accessible constructor available.", implementation.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                throw HandledException.Shared;
            }

            var methodsBuilder = ImmutableArray.CreateBuilder<DpMethod>();
            var fieldsBuilder = ImmutableArray.CreateBuilder<DpField>();
            var propertiesBuilder = ImmutableArray.CreateBuilder<DpProperty>();
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
                                methodsBuilder.Add(new DpMethod(method, ordinal.Value, GetParameters(setup, method.Parameters, compilation, setup.TypeConstructor)));
                            }
                        }

                        break;

                    case IFieldSymbol field:
                        if (field is { IsReadOnly: false, IsStatic: false, IsConst: false })
                        {
                            var ordinal = GetAttribute(setup.OrdinalAttributes, member, default(int?));
                            if (ordinal.HasValue || field.IsRequired)
                            {
                                fieldsBuilder.Add(
                                    new DpField(
                                        field,
                                        ordinal,
                                        new Injection(
                                            GetAttribute(setup.TypeAttributes, field, setup.TypeConstructor?.Construct(compilation, field.Type) ?? field.Type),
                                            GetAttribute(setup.TagAttributes, field, default(object?)))));
                            }
                        }

                        break;

                    case IPropertySymbol property:
                        if (property is { IsReadOnly: false, IsStatic: false, IsIndexer: false })
                        {
                            var ordinal = GetAttribute(setup.OrdinalAttributes, member, default(int?));
                            if (ordinal.HasValue || property.IsRequired)
                            {
                                propertiesBuilder.Add(
                                    new DpProperty(
                                        property,
                                        ordinal,
                                        new Injection(
                                            GetAttribute(setup.TypeAttributes, property, setup.TypeConstructor?.Construct(compilation, property.Type) ?? property.Type),
                                            GetAttribute(setup.TagAttributes, property, default(object?)))));
                            }
                        }

                        break;
                }
            }

            var variantId = 0;
            foreach (var constructor in constructors)
            {
                var dpImplementation = new DpImplementation(
                    implementation,
                    binding,
                    constructor,
                    methodsBuilder.ToImmutable(),
                    propertiesBuilder.ToImmutable(),
                    fieldsBuilder.ToImmutable());
                
                yield return new DependencyNode(variantId++, Implementation: dpImplementation);
            }
        }
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
            dependenciesBuilder.Add(
                new DpParameter(
                    parameter,
                    new Injection(
                        GetAttribute(setup.TypeAttributes, parameter, typeConstructor?.Construct(compilation, parameter.Type) ?? parameter.Type),
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
            var attributeData = attribute.SemanticModel.Compilation.GetAttributes(member, attribute.AttributeType);
            switch (attributeData.Count)
            {
                case 1:
                    var args = attributeData[0].ConstructorArguments;
                    if (attribute.ArgumentPosition > args.Length)
                    {
                        _logger.CompileError($"The argument position {attribute.ArgumentPosition.ToString()} of attribute {attribute.Source} is out of range [0..{args.Length.ToString()}].", attribute.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                    }

                    var typedConstant = args[attribute.ArgumentPosition];
                    if (typedConstant.Value is T value)
                    {
                        return value;
                    }

                    break;

                case > 1:
                    _logger.CompileError($"{member} of the type {member.ContainingType} cannot be processed because it is marked with multiple mutually exclusive attributes.", attribute.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                    throw HandledException.Shared;
            }
        }

        return defaultValue;
    }
}