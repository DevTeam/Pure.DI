// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

using static Tag;

internal class TypeResolver(
    IMarker marker,
    [Tag(GenericType)] IIdGenerator idGenerator,
    ISymbolNames symbolNames)
    : ITypeResolver
{
    private readonly Dictionary<ITypeSymbol, string> _names = new(SymbolEqualityComparer.Default);

    public TypeDescription Resolve(MdSetup setup, ITypeSymbol type) => Resolve(setup, type, null);

    private TypeDescription Resolve(MdSetup setup, ITypeSymbol type, ITypeParameterSymbol? typeParam)
    {
        TypeDescription description;
        switch (type)
        {
            case INamedTypeSymbol { IsGenericType: false }:
                if (marker.IsMarker(setup, type))
                {
                    if (!_names.TryGetValue(type, out var typeName))
                    {
                        var id = idGenerator.Generate();
                        typeName = id == 0 ? "T" : $"T{id}";
                        _names.Add(type, typeName);
                    }

                    description = new TypeDescription(typeName, ImmutableArray.Create(new TypeDescription(typeName, ImmutableArray<TypeDescription>.Empty, typeParam)), typeParam);
                }
                else
                {
                    description = new TypeDescription(symbolNames.GetGlobalName(type), ImmutableArray<TypeDescription>.Empty, typeParam);
                }

                break;

            case INamedTypeSymbol { IsTupleType: true } tupleTypeSymbol:
            {
                var elements = new List<string>();
                var args = new List<TypeDescription>();
                foreach (var item in tupleTypeSymbol.TupleElements.Zip(tupleTypeSymbol.TypeParameters, (element, parameter) => (description: Resolve(setup, element.Type, parameter), element)))
                {
                    elements.Add($"{item.description} {item.element.Name}");
                    args.AddRange(item.description.TypeArgs);
                }

                description = new TypeDescription($"({string.Join(", ", elements)})", args.Distinct().ToList(), typeParam);
            }
                break;

            case INamedTypeSymbol namedTypeSymbol:
            {
                var types = new List<string>();
                var args = new List<TypeDescription>();
                foreach (var typeArgDescription in namedTypeSymbol.TypeArguments.Zip(namedTypeSymbol.TypeParameters, (symbol, parameterSymbol) => Resolve(setup, symbol, parameterSymbol)))
                {
                    args.AddRange(typeArgDescription.TypeArgs);
                    types.Add(typeArgDescription.Name);
                }

                var name = string.Join("", namedTypeSymbol.ToDisplayParts().TakeWhile(i => i.ToString() != "<"));
                description = new TypeDescription($"{name}<{string.Join(", ", types)}>", args.Distinct().ToList(), typeParam);
            }
                break;

            case IArrayTypeSymbol arrayTypeSymbol:
                var arrayDescription = Resolve(setup, arrayTypeSymbol.ElementType);
                description = arrayDescription with { Name = $"{arrayDescription.Name}[]" };
                break;

            default:
                description = new TypeDescription(symbolNames.GetGlobalName(type), ImmutableArray<TypeDescription>.Empty, typeParam);
                break;
        }

        return description;
    }
}