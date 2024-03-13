namespace Pure.DI.Core.Code;

internal class TypeResolver(IMarker marker)
    : ITypeResolver
{
    private readonly Dictionary<ITypeSymbol, TypeDescription> _typeParameters = new(SymbolEqualityComparer.Default);
    private int _markerCounter;

    public TypeDescription Resolve(ITypeSymbol type)
    {
        if (_typeParameters.TryGetValue(type, out var description))
        {
            return description;
        }
        
        switch (type)
        {
            case INamedTypeSymbol { IsGenericType: false }:
                if (marker.IsMarker(type))
                {
                    var typeName = _markerCounter == 0 ? "T" : $"T{_markerCounter}";
                    _markerCounter++;
                    description = new TypeDescription(typeName, ImmutableArray.Create(new TypeDescription(typeName, ImmutableArray<TypeDescription>.Empty)));
                }
                else
                {
                    description = new TypeDescription(type.ToString(), ImmutableArray<TypeDescription>.Empty);
                }
                
                break;

            case INamedTypeSymbol { IsTupleType: true } tupleTypeSymbol:
                {
                    var elements = new List<string>();
                    var args = new List<TypeDescription>();
                    foreach (var tupleElement in tupleTypeSymbol.TupleElements)
                    {
                        var tupleElementDescription = Resolve(tupleElement.Type);
                        elements.Add($"{tupleElementDescription} {tupleElement.Name}");
                        args.AddRange(tupleElementDescription.TypeArgs);
                    }
                    
                    description = new TypeDescription($"({string.Join(", ", elements)})", args.Distinct().ToImmutableArray());
                }
                break;

            case INamedTypeSymbol namedTypeSymbol:
                {
                    var types = new List<string>();
                    var args = new List<TypeDescription>();
                    foreach (var typeArgDescription in namedTypeSymbol.TypeArguments.Select(Resolve))
                    {
                        args.AddRange(typeArgDescription.TypeArgs);
                        types.Add(typeArgDescription.Name);
                    }

                    var name = string.Join("", namedTypeSymbol.ToDisplayParts().TakeWhile(i => i.ToString() != "<"));
                    description = new TypeDescription($"{name}<{string.Join(", ", types)}>", args.Distinct().ToImmutableArray());
                }
                break;

            case IArrayTypeSymbol arrayTypeSymbol:
                var arrayDescription = Resolve(arrayTypeSymbol.ElementType);
                description = arrayDescription with { Name =  $"{arrayDescription.Name}[]" };
                break;
            
            default:
                description = new TypeDescription(type.ToString(), ImmutableArray<TypeDescription>.Empty);
                break;
        }
        
        _typeParameters.Add(type, description);
        return description;
    }
}