namespace Pure.DI.Core;

sealed class TypeSymbolComparer : ITypeSymbolComparer
{
    private static readonly SymbolDisplayFormat DependencyFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public IEqualityComparer<ITypeSymbol> Runtime { get; } = new RuntimeTypeComparer();

    public IEqualityComparer<ITypeSymbol> Dependency { get; } = new DependencyTypeComparer();

    public bool RuntimeEquals(ITypeSymbol? type, ITypeSymbol? otherType) =>
        type is null
            ? otherType is null
            : otherType is not null && Runtime.Equals(type, otherType);

    public bool DependencyEquals(ITypeSymbol? type, ITypeSymbol? otherType) =>
        type is null
            ? otherType is null
            : otherType is not null && Dependency.Equals(type, otherType);

    public int GetRuntimeHashCode(ITypeSymbol type) =>
        Runtime.GetHashCode(type);

    public int GetDependencyHashCode(ITypeSymbol type) =>
        Dependency.GetHashCode(type);

    private sealed class DependencyTypeComparer : IEqualityComparer<ITypeSymbol>
    {
        private readonly Dictionary<ITypeSymbol, string> _keys = new(SymbolEqualityComparer.IncludeNullability);

        public bool Equals(ITypeSymbol? x, ITypeSymbol? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return GetDependencyKey(x) == GetDependencyKey(y);
        }

        public int GetHashCode(ITypeSymbol obj) =>
            GetDependencyKey(obj).GetHashCode();

        private string GetDependencyKey(ITypeSymbol type)
        {
            if (_keys.TryGetValue(type, out var key))
            {
                return key;
            }

            key = CreateDependencyKey(type);
            _keys.Add(type, key);
            return key;
        }
    }

    private sealed class RuntimeTypeComparer : IEqualityComparer<ITypeSymbol>
    {
        public bool Equals(ITypeSymbol? x, ITypeSymbol? y) =>
            SymbolEqualityComparer.Default.Equals(Normalize(x), Normalize(y));

        public int GetHashCode(ITypeSymbol obj) =>
            SymbolEqualityComparer.Default.GetHashCode(Normalize(obj));

        private static ITypeSymbol? Normalize(ITypeSymbol? type) =>
            type is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated }
                ? type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                : type;
    }

    private static string CreateDependencyKey(ITypeSymbol type) =>
        $"{type.ToDisplayString(NullableFlowState.None, DependencyFormat)}{GetTopLevelNullableMarker(type)}";

    private static string GetTopLevelNullableMarker(ITypeSymbol type) =>
        type is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated } ? "?" : "";
}
