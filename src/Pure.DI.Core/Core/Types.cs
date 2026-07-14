// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class Types(
    ICache<Types.SpecialTypeKey, INamedTypeSymbol?> specialTypes,
    ICache<Types.TypeSymbolKey, string> names,
    ICache<Types.GlobalTypeSymbolKey, string> globalNames,
    ITypeSymbolComparer typeSymbolComparer)
    : ITypes, ISymbolNames
{
    private static readonly Dictionary<SpecialType, string> TypeShortNames = new()
    {
        { SpecialType.IAsyncDisposable, $"{nameof(System)}.IAsyncDisposable" },
        { SpecialType.CompositionKind, $"{Names.GeneratorName}.{nameof(CompositionKind)}" },
        { SpecialType.Lifetime, $"{Names.GeneratorName}.{nameof(Lifetime)}" },
        { SpecialType.Tag, $"{Names.GeneratorName}.{nameof(Tag)}" },
        { SpecialType.IConfiguration, $"{Names.GeneratorName}.{nameof(IConfiguration)}" },
        { SpecialType.Func, "System.Func`1" },
        { SpecialType.LightweightRoot, Names.LightweightRootBaseClassName },
        { SpecialType.OverloadResolutionPriorityAttribute, "System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute" },
        { SpecialType.IUnion, "System.Runtime.CompilerServices.IUnion" },
        { SpecialType.UnionAttribute, "System.Runtime.CompilerServices.UnionAttribute" }
    };

    public string GetName(ITypeSymbol typeSymbol) =>
        names.Get(new TypeSymbolKey(typeSymbol), key => key.TypeSymbol.ToString());

    public string GetGlobalName(ITypeSymbol typeSymbol) =>
        globalNames.Get(new GlobalTypeSymbolKey(typeSymbol), key => key.TypeSymbol.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat));

    public INamedTypeSymbol? TryGet(SpecialType specialType, Compilation compilation) =>
        specialTypes.Get(
            new SpecialTypeKey(specialType, compilation),
            i => i.Compilation.GetTypeByMetadataName(TypeShortNames[specialType]));

    public bool TypeEquals(ISymbol? type1, ISymbol? type2) =>
        type1 is ITypeSymbol typeSymbol1
        && type2 is ITypeSymbol typeSymbol2
        && typeSymbolComparer.RuntimeEquals(typeSymbol1, typeSymbol2);

    public bool IsUnionType(Compilation compilation, ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        if (namedType.OriginalDefinition.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Nullable_T
            && namedType.TypeArguments is [INamedTypeSymbol underlyingType])
        {
            namedType = underlyingType;
        }

        if (namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return false;
        }

        if (TryGet(SpecialType.IUnion, compilation) is {} unionInterface
            && namedType.AllInterfaces.Any(i => typeSymbolComparer.RuntimeEquals(i, unionInterface)))
        {
            return true;
        }

        return TryGet(SpecialType.UnionAttribute, compilation) is {} unionAttribute
               && namedType.GetAttributes().Any(i => i.AttributeClass is {} attributeClass && typeSymbolComparer.RuntimeEquals(attributeClass, unionAttribute));
    }

    public bool IsImplicitUnionConversion(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
    {
#if ROSLYN5_6_OR_GREATER
#pragma warning disable RSEXPERIMENTAL006
        var conversion = compilation.ClassifyConversion(sourceType, targetType);
        return conversion.IsImplicit
               && (conversion.IsUnion
                   || conversion.IsUserDefined
                   && IsUnionType(compilation, targetType)
                   && IsUnionCaseSource(compilation, sourceType, targetType));
#pragma warning restore RSEXPERIMENTAL006
#else
        // Fallback for Roslyn versions without Conversion.IsUnion:
        // the target must be a union type detected via the well-known types
        return IsUnionType(compilation, targetType)
               && compilation.ClassifyConversion(sourceType, targetType).IsImplicit
               && IsUnionCaseSource(compilation, sourceType, targetType);
#endif
    }

    private bool IsUnionCaseSource(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        if (targetType is not INamedTypeSymbol unionType)
        {
            return false;
        }

        if (unionType.OriginalDefinition.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Nullable_T
            && unionType.TypeArguments is [INamedTypeSymbol underlyingType])
        {
            unionType = underlyingType;
        }

        var memberProvider = unionType.GetTypeMembers("IUnionMembers")
            .FirstOrDefault(i => i.TypeKind == TypeKind.Interface);

        IEnumerable<ITypeSymbol> caseTypes = memberProvider is null
            ? unionType.InstanceConstructors
                .Where(i => i.DeclaredAccessibility == Accessibility.Public && i.Parameters.Length == 1)
                .Select(i => i.Parameters[0].Type)
            : memberProvider.GetMembers("Create")
                .OfType<IMethodSymbol>()
                .Where(i => i is { IsStatic: true, DeclaredAccessibility: Accessibility.Public, Parameters.Length: 1 }
                            && typeSymbolComparer.RuntimeEquals(i.ReturnType, unionType))
                .Select(i => i.Parameters[0].Type);

        return caseTypes.Any(caseType =>
        {
            var caseConversion = compilation.ClassifyConversion(sourceType, caseType);
            return caseConversion.IsImplicit && !caseConversion.IsUserDefined && !IsUnionConversion(caseConversion);
        });
    }

    private static bool IsUnionConversion(Conversion conversion)
    {
#if ROSLYN5_6_OR_GREATER
#pragma warning disable RSEXPERIMENTAL006
        return conversion.IsUnion;
#pragma warning restore RSEXPERIMENTAL006
#else
        return false;
#endif
    }

    internal readonly struct SpecialTypeKey(SpecialType specialType, Compilation compilation) : IEquatable<SpecialTypeKey>
    {
        private readonly SpecialType _specialType = specialType;
        public readonly Compilation Compilation = compilation;

        public bool Equals(SpecialTypeKey other) => _specialType == other._specialType && Compilation.Equals(other.Compilation);

        public override bool Equals(object? obj) => obj is SpecialTypeKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (int)_specialType * 397 ^ Compilation.GetHashCode();
            }
        }
    }

    internal readonly struct TypeSymbolKey(ITypeSymbol typeSymbol) : IEquatable<TypeSymbolKey>
    {
        public readonly ITypeSymbol TypeSymbol = typeSymbol;

        public bool Equals(TypeSymbolKey other) => SymbolEqualityComparer.IncludeNullability.Equals(TypeSymbol, other.TypeSymbol);

        public override bool Equals(object? obj) => obj is TypeSymbolKey other && Equals(other);

        public override int GetHashCode() => SymbolEqualityComparer.IncludeNullability.GetHashCode(TypeSymbol);
    }

    internal readonly struct GlobalTypeSymbolKey(ITypeSymbol typeSymbol) : IEquatable<GlobalTypeSymbolKey>
    {
        public readonly ITypeSymbol TypeSymbol = typeSymbol;

        public bool Equals(GlobalTypeSymbolKey other) => SymbolEqualityComparer.IncludeNullability.Equals(TypeSymbol, other.TypeSymbol);

        public override bool Equals(object? obj) => obj is GlobalTypeSymbolKey other && Equals(other);

        public override int GetHashCode() => SymbolEqualityComparer.IncludeNullability.GetHashCode(TypeSymbol);
    }
}
