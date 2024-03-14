// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class TypeResolver(IMarker marker)
    : ITypeResolver
{
    private readonly Dictionary<ITypeSymbol, string> _names = new(SymbolEqualityComparer.Default);
    private int _markerCounter;

    public TypeDescription Resolve(ITypeSymbol type) => Resolve(type, default);

    private TypeDescription Resolve(ITypeSymbol type, ITypeParameterSymbol? typeParam)
    {
        TypeDescription description;
        switch (type)
        {
            case INamedTypeSymbol { IsGenericType: false }:
                if (marker.IsMarker(type))
                {
                    if (!_names.TryGetValue(type, out var typeName))
                    {
                        typeName = _markerCounter == 0 ? "T" : $"T{_markerCounter}";
                        _names.Add(type, typeName);
                    }
                    
                    _markerCounter++;
                    description = new TypeDescription(typeName, ImmutableArray.Create(new TypeDescription(typeName, ImmutableArray<TypeDescription>.Empty, typeParam)), typeParam);
                }
                else
                {
                    description = new TypeDescription(type.ToString(), ImmutableArray<TypeDescription>.Empty, typeParam);
                }
                
                break;

            case INamedTypeSymbol { IsTupleType: true } tupleTypeSymbol:
                {
                    var elements = new List<string>();
                    var args = new List<TypeDescription>();
                    foreach (var item in tupleTypeSymbol.TupleElements.Zip(tupleTypeSymbol.TypeParameters, (element, parameter) => (description: Resolve(element.Type, parameter), element)))
                    {
                        elements.Add($"{item.description} {item.element.Name}");
                        args.AddRange(item.description.TypeArgs);
                    }
                    
                    description = new TypeDescription($"({string.Join(", ", elements)})", args.Distinct().ToImmutableArray(), typeParam);
                }
                break;

            case INamedTypeSymbol namedTypeSymbol:
                {
                    var types = new List<string>();
                    var args = new List<TypeDescription>();
                    foreach (var typeArgDescription in namedTypeSymbol.TypeArguments.Zip(namedTypeSymbol.TypeParameters, Resolve))
                    {
                        args.AddRange(typeArgDescription.TypeArgs);
                        types.Add(typeArgDescription.Name);
                    }

                    var name = string.Join("", namedTypeSymbol.ToDisplayParts().TakeWhile(i => i.ToString() != "<"));
                    description = new TypeDescription($"{name}<{string.Join(", ", types)}>", args.Distinct().ToImmutableArray(), typeParam);
                }
                break;

            case IArrayTypeSymbol arrayTypeSymbol:
                var arrayDescription = Resolve(arrayTypeSymbol.ElementType);
                description = arrayDescription with { Name =  $"{arrayDescription.Name}[]" };
                break;
            
            default:
                description = new TypeDescription(type.ToString(), ImmutableArray<TypeDescription>.Empty, typeParam);
                break;
        }
        
        return description;
    }
}