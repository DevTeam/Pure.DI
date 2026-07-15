// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class Types(
    ICache<Types.SpecialTypeKey, INamedTypeSymbol?> specialTypes,
    ICache<Types.TypeSymbolKey, string> names,
    ICache<Types.GlobalTypeSymbolKey, string> globalNames,
    ICache<Types.UnionCasesKey, ImmutableArray<ITypeSymbol>> unionCases,
    ICache<Types.UnionConversionKey, bool> unionConversions,
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
        => unionConversions.Get(
            new UnionConversionKey(compilation, sourceType, targetType),
            key => ClassifyUnionConversion(key.Compilation, key.SourceType, key.TargetType));

    public ImmutableArray<ITypeSymbol> GetUnionCaseTypes(Compilation compilation, ITypeSymbol type) =>
        unionCases.Get(new UnionCasesKey(compilation, type), key => CreateUnionCaseTypes(key.Type));

    private bool ClassifyUnionConversion(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
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
        return GetUnionCaseTypes(compilation, targetType).Any(caseType =>
        {
            var caseConversion = compilation.ClassifyConversion(sourceType, caseType);
            return caseConversion is { IsImplicit: true, IsUserDefined: false } && !IsUnionConversion(caseConversion);
        });
    }

    private ImmutableArray<ITypeSymbol> CreateUnionCaseTypes(ITypeSymbol targetType)
    {
        if (targetType is not INamedTypeSymbol unionType)
        {
            return ImmutableArray<ITypeSymbol>.Empty;
        }

        if (unionType.OriginalDefinition.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Nullable_T
            && unionType.TypeArguments is [INamedTypeSymbol underlyingType])
        {
            unionType = underlyingType;
        }

        var memberProvider = unionType.GetTypeMembers("IUnionMembers")
            .FirstOrDefault(i =>
                i is { TypeKind: TypeKind.Interface, DeclaredAccessibility: Accessibility.Public }
                && unionType.AllInterfaces.Any(j => typeSymbolComparer.RuntimeEquals(i, j)));

        return (memberProvider is null
                ? unionType.InstanceConstructors
                    .Where(i =>
                        i.DeclaredAccessibility == Accessibility.Public
                        && i.Parameters is [{ RefKind: RefKind.None or RefKind.In }])
                    .Select(i => i.Parameters[0].Type)
                : memberProvider.GetMembers("Create")
                    .OfType<IMethodSymbol>()
                    .Where(i => i is
                                {
                                    IsStatic: true,
                                    DeclaredAccessibility: Accessibility.Public,
                                    Parameters: [{ RefKind: RefKind.None or RefKind.In }]
                                }
                                && typeSymbolComparer.RuntimeEquals(i.ReturnType, unionType))
                    .Select(i => i.Parameters[0].Type))
            .ToImmutableArray();
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

    internal readonly struct UnionCasesKey(Compilation compilation, ITypeSymbol type) : IEquatable<UnionCasesKey>
    {
        private readonly Compilation _compilation = compilation;
        public readonly ITypeSymbol Type = type;

        public bool Equals(UnionCasesKey other) =>
            ReferenceEquals(_compilation, other._compilation)
            && SymbolEqualityComparer.IncludeNullability.Equals(Type, other.Type);

        public override bool Equals(object? obj) => obj is UnionCasesKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return _compilation.GetHashCode() * 397 ^ SymbolEqualityComparer.IncludeNullability.GetHashCode(Type);
            }
        }
    }

    internal readonly struct UnionConversionKey(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType) : IEquatable<UnionConversionKey>
    {
        public readonly Compilation Compilation = compilation;
        public readonly ITypeSymbol SourceType = sourceType;
        public readonly ITypeSymbol TargetType = targetType;

        public bool Equals(UnionConversionKey other) =>
            ReferenceEquals(Compilation, other.Compilation)
            && SymbolEqualityComparer.IncludeNullability.Equals(SourceType, other.SourceType)
            && SymbolEqualityComparer.IncludeNullability.Equals(TargetType, other.TargetType);

        public override bool Equals(object? obj) => obj is UnionConversionKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Compilation.GetHashCode();
                hashCode = hashCode * 397 ^ SymbolEqualityComparer.IncludeNullability.GetHashCode(SourceType);
                hashCode = hashCode * 397 ^ SymbolEqualityComparer.IncludeNullability.GetHashCode(TargetType);
                return hashCode;
            }
        }
    }
}
