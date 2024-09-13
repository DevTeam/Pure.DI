// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

internal class NodeInfo(IAsyncDisposableSettings asyncDisposableSettings) : INodeInfo
{
    public bool IsDelegate(DependencyNode node) =>
        node.Type.TypeKind == TypeKind.Delegate;

    public bool IsLazy(DependencyNode node) =>
        IsDelegate(node) || IsEnumerable(node) || IsAsyncEnumerable(node);

    public bool IsDisposable(DependencyNode node) =>
        node.Type.AllInterfaces.Any(i =>
            i.SpecialType == SpecialType.System_IDisposable
            || IsAsyncDisposable(node.Binding.SemanticModel.Compilation, i));

    public bool IsAsyncDisposable(DependencyNode node) =>
        node.Type.AllInterfaces.Any(i => IsAsyncDisposable(node.Binding.SemanticModel.Compilation, i));

    private static bool IsEnumerable(DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.Enumerable };

    private static bool IsAsyncEnumerable(DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.AsyncEnumerable };

    private bool IsAsyncDisposable(Compilation compilation, ISymbol type) =>
        asyncDisposableSettings.TryGetAsyncDisposableType(compilation) is { } asyncDisposableType
        && SymbolEqualityComparer.Default.Equals(type, asyncDisposableType);
}