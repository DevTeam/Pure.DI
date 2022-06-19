// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable MergeIntoPattern
// ReSharper disable MergeIntoNegatedPattern
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Pure.DI.Core;

using NS35EBD81B;

internal class SemanticType
{
    public const string GenericTypeArgumentPrefix = "TT";
    public readonly ITypeSymbol Type;
    public readonly SemanticModel SemanticModel;

    public SemanticType(ITypeSymbol type, SemanticModel semanticModel)
    {
        Type = type;
        SemanticModel = semanticModel;
    }

    public static implicit operator SemanticModel(SemanticType semanticType) => semanticType.SemanticModel;

    public static implicit operator TypeSyntax(SemanticType semanticType) => SyntaxFactory.ParseTypeName(semanticType.Type.ToMinimalDisplayString(semanticType.SemanticModel, 0));

    public bool Equals(SemanticType other) => SymbolEqualityComparer.Default.Equals(Type, other.Type);

    public override string ToString() => Type.ToString();

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == GetType() && Equals((SemanticType)obj);
    }

    public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(Type);

    public string Name
    {
        get
        {
            if (
                Type is not INamedTypeSymbol namedTypeSymbol
                || !namedTypeSymbol.IsGenericType)
            {
                return string.Join("", GetParts(Type));
            }

            var unboundGeneric = namedTypeSymbol.ConstructUnboundGenericType();
            return string.Join("", GetParts(unboundGeneric).TakeWhile(i => i.ToString() != "<")) + "`" + namedTypeSymbol.TypeArguments.Length;
        }
    }

    private static IEnumerable<string> GetParts(ISymbol symbol)
    {
#pragma warning disable 219
        var isNested = false;
#pragma warning restore 219
        foreach (var part in symbol.ToDisplayParts())
        {
            switch (part.Kind)
            {
                case SymbolDisplayPartKind.ClassName:
                    isNested = true;
                    yield return part.ToString();
                    break;

                case SymbolDisplayPartKind.Punctuation:
                    if (isNested && part.ToString() == ".")
                    {
                        yield return "+";
                    }
                    else
                    {
                        yield return part.ToString();
                    }

                    break;

                default:
                    yield return part.ToString();
                    break;
            }
        }
    }

    public SemanticType Construct(params SemanticType[] typeArg)
    {
        if (
            Type is not INamedTypeSymbol namedTypeSymbol
            || !namedTypeSymbol.IsGenericType)
        {
            throw new ArgumentException($"{Type} is not a generic type.");
        }

        var unboundGeneric = namedTypeSymbol.ConstructUnboundGenericType();
        var unboundGenericType = new SemanticType(unboundGeneric, SemanticModel);
        var typeName = unboundGenericType.Name;
        unboundGeneric = (
            from ns in new[]
            {
                string.Empty
            }.Concat(SemanticModel.LookupNamespacesAndTypes(0).Select(i => $"{i}."))
            let generic = SemanticModel.Compilation.GetTypeByMetadataName($"{ns}{typeName}")
            where generic != null
            select generic).FirstOrDefault();

        if (unboundGeneric == null)
        {
            throw new InvalidOperationException($"Cannot construct {Type}.");
        }

        return new SemanticType(unboundGeneric.Construct(typeArg.Select(i => i.Type).ToArray()), SemanticModel);
    }

    public SemanticType ConstructUnbound()
    {
        switch (Type)
        {
            case INamedTypeSymbol namedTypeSymbol:
                return IsComposedGenericTypeMarker && namedTypeSymbol.IsGenericType ? new SemanticType(namedTypeSymbol.ConstructUnboundGenericType(), SemanticModel) : this;

            case IArrayTypeSymbol arrayTypeSymbol:
                var elementType = new SemanticType(arrayTypeSymbol.ElementType, SemanticModel).ConstructUnbound();
                var arrayType = SemanticModel.Compilation.CreateArrayTypeSymbol(elementType.Type);
                return new SemanticType(arrayType, SemanticModel);

            default:
                return this;
        }
    }

    public bool IsGenericTypeMarker
    {
        get
        {
            if (!Type.Name.StartsWith(GenericTypeArgumentPrefix))
            {
                return false;
            }

            foreach (var attr in Type.GetAttributes())
            {
                if (
                    attr.AttributeClass != null
                    && attr.AttributeClass.Name == nameof(GenericTypeArgumentAttribute))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool IsComposedGenericTypeMarker
    {
        get
        {
            if (IsGenericTypeMarker)
            {
                return true;
            }

            return Type switch
            {
                INamedTypeSymbol { IsGenericType: false } => false,
                INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.TypeArguments.Any(type => new SemanticType(type, SemanticModel).IsComposedGenericTypeMarker),
                IArrayTypeSymbol arrayTypeSymbol => new SemanticType(arrayTypeSymbol.ElementType, SemanticModel).IsComposedGenericTypeMarker,
                _ => false
            };
        }
    }

    public bool IsValidTypeToResolve =>
        Type switch
        {
            INamedTypeSymbol namedTypeSymbol => !namedTypeSymbol.IsUnboundGenericType && !IsComposedGenericTypeMarker && !namedTypeSymbol.IsRefLikeType,
            IArrayTypeSymbol arrayTypeSymbol => new SemanticType(arrayTypeSymbol.ElementType, SemanticModel).IsValidTypeToResolve,
            _ => false
        };

    public bool Equals(Type type) => SymbolEqualityComparer.Default.Equals(ToTypeSymbol(type), Type);

    public TypeSyntax TypeSyntax => SyntaxFactory.ParseTypeName(Type.ToMinimalDisplayString(SemanticModel, 0));

    public bool ImplementsInterface<T>()
    {
        var interfaceType = SemanticModel.Compilation.GetTypeByMetadataName(typeof(T).FullName.ReplaceNamespace());
        return interfaceType != null && ImplementsInterface(interfaceType);
    }

    private bool ImplementsInterface(ISymbol interfaceType) =>
        Type.AllInterfaces.Any(i => i.Equals(interfaceType, SymbolEqualityComparer.Default));
    
    public bool Implements(ISymbol interfaceType) =>
        GetBaseTypes(Type).Any(i => i.Equals(interfaceType, SymbolEqualityComparer.Default));

    private static IEnumerable<ITypeSymbol> GetBaseTypes(ITypeSymbol symbol)
    {
        while (true)
        {
            yield return symbol;
            foreach (var type in symbol.AllInterfaces.SelectMany(GetBaseTypes))
            {
                yield return type;
            }

            if (symbol.BaseType != default)
            {
                symbol = symbol.BaseType;
                continue;
            }

            break;
        }
    }

    private INamedTypeSymbol ToTypeSymbol(Type type)
    {
        if (type.FullName == null)
        {
            throw new ArgumentException(nameof(type));
        }

        if (type.IsGenericTypeDefinition)
        {
            var typeName = type.FullName.ReplaceNamespace();
            return SemanticModel.Compilation.GetTypeByMetadataName(typeName)?.ConstructUnboundGenericType() ?? throw new InvalidOperationException($"Cannot convert type {typeName}.");
        }

        if (!type.IsConstructedGenericType)
        {
            var typeName = type.FullName.ReplaceNamespace();
            return SemanticModel.Compilation.GetTypeByMetadataName(typeName) ?? throw new InvalidOperationException($"Cannot convert type {typeName}.");
        }

        var typeArgumentsTypeInfos = type.GenericTypeArguments.Select(ToTypeSymbol);
        var openType = type.GetGenericTypeDefinition();
        var openTypeName = openType.FullName?.ReplaceNamespace();
        var typeSymbol = SemanticModel.Compilation.GetTypeByMetadataName(openTypeName ?? throw new InvalidOperationException($"Cannot get generic type name {openTypeName}."));
        return typeSymbol?.Construct(typeArgumentsTypeInfos.ToArray<ITypeSymbol>()) ?? throw new InvalidOperationException("Cannot convert generic type.");
    }
}