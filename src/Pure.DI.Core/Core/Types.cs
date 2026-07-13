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
        { SpecialType.OverloadResolutionPriorityAttribute, "System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute" }
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
