// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.

namespace Pure.DI.Core.Code;

sealed class TypeResolver(
    IMarker marker,
    IUniqueNameProvider uniqueNameProvider,
    ISymbolNames symbolNames,
    ITypes types,
    ITypeSymbolComparer typeSymbolComparer)
    : ITypeResolver
{
    private readonly Dictionary<ITypeSymbol, string> _names = new(typeSymbolComparer.Runtime);

    public TypeDescription Resolve(MdSetup setup, ITypeSymbol type) => Resolve(setup, type, null, true);

    public TypeDescription ResolveRuntime(MdSetup setup, ITypeSymbol type) => Resolve(setup, type, null, false);

    private TypeDescription Resolve(MdSetup setup, ITypeSymbol type, ITypeParameterSymbol? typeParam, bool includeNullableReferenceTypes)
    {
        if (typeSymbolComparer.RuntimeEquals(types.TryGet(SpecialType.LightweightRoot, setup.SemanticModel.Compilation), type))
        {
            return new TypeDescription(Names.LightweightRootClassName, ImmutableArray<TypeDescription>.Empty, null);
        }

        TypeDescription description;
        switch (type)
        {
            case INamedTypeSymbol { IsGenericType: false }:
                if (marker.IsMarker(setup, type))
                {
                    if (!_names.TryGetValue(type, out var typeName))
                    {
                        typeName = uniqueNameProvider.GetUniqueName("T");
                        _names.Add(type, typeName);
                    }

                    description = new TypeDescription(typeName, ImmutableArray.Create(new TypeDescription(typeName, ImmutableArray<TypeDescription>.Empty, typeParam)), typeParam);
                }
                else
                {
                    description = new TypeDescription(GetGlobalName(type, includeNullableReferenceTypes), ImmutableArray<TypeDescription>.Empty, typeParam);
                }

                break;

            case INamedTypeSymbol { IsTupleType: true } tupleTypeSymbol:
            {
                var elements = new List<string>();
                var args = new List<TypeDescription>();
                foreach (var item in tupleTypeSymbol.TupleElements.Zip(tupleTypeSymbol.TypeParameters, (element, parameter) => (description: Resolve(setup, element.Type, parameter, includeNullableReferenceTypes), element)))
                {
                    elements.Add($"{item.description} {item.element.Name}");
                    args.AddRange(item.description.TypeArgs);
                }

                description = new TypeDescription($"({string.Join(", ", elements)})", args.Distinct().ToList(), typeParam);
            }
                break;

            case INamedTypeSymbol
            {
                ConstructedFrom.SpecialType: Microsoft.CodeAnalysis.SpecialType.System_Nullable_T,
                TypeArguments: [{} nullableValueType]
            }:
            {
                var nullableValueTypeDescription = Resolve(setup, nullableValueType);
                description = nullableValueTypeDescription with { Name = $"{nullableValueTypeDescription.Name}?" };
            }
                break;

            case INamedTypeSymbol namedTypeSymbol:
            {
                var typeArgs = new List<string>();
                var args = new List<TypeDescription>();
                foreach (var typeArgDescription in namedTypeSymbol.TypeArguments.Zip(namedTypeSymbol.TypeParameters, (symbol, parameterSymbol) => Resolve(setup, symbol, parameterSymbol, includeNullableReferenceTypes)))
                {
                    args.AddRange(typeArgDescription.TypeArgs);
                    typeArgs.Add(typeArgDescription.Name);
                }

                var name = string.Join("", namedTypeSymbol.ToDisplayParts().TakeWhile(i => i.ToString() != "<"));
                var genericNullableSuffix = includeNullableReferenceTypes && namedTypeSymbol is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated } ? "?" : "";
                description = new TypeDescription($"{name}<{string.Join(", ", typeArgs)}>{genericNullableSuffix}", args.Distinct().ToList(), typeParam);
            }
                break;

            case IArrayTypeSymbol arrayTypeSymbol:
                var arrayDescription = Resolve(setup, arrayTypeSymbol.ElementType, null, includeNullableReferenceTypes);
                var nullableSuffix = includeNullableReferenceTypes && arrayTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
                description = arrayDescription with { Name = $"{arrayDescription.Name}[]{nullableSuffix}" };
                break;

            default:
                description = new TypeDescription(GetGlobalName(type, includeNullableReferenceTypes), ImmutableArray<TypeDescription>.Empty, typeParam);
                break;
        }

        return description;
    }

    private string GetGlobalName(ITypeSymbol type, bool includeNullableReferenceTypes) => includeNullableReferenceTypes && type is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated }
        ? $"{symbolNames.GetGlobalName(type)}?"
        : symbolNames.GetGlobalName(type);
}
