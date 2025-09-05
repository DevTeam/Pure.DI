// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

using static Lifetime;
using SpecialType=Microsoft.CodeAnalysis.SpecialType;

sealed class NodeTools(ITypes types) : INodeTools
{
    public bool IsLazy(DependencyNode node) =>
        IsDelegate(node) || IsEnumerable(node) || IsAsyncEnumerable(node);

    public bool IsBlock(IDependencyNode node) =>
        node.Lifetime is Singleton or Scoped or PerResolve;

    public bool IsDisposableAny(DependencyNode node) =>
        node.Type.AllInterfaces.Any(i =>
            i.SpecialType == SpecialType.System_IDisposable
            || IsAsyncDisposable(node.Binding.SemanticModel.Compilation, i));

    public bool IsDisposable(DependencyNode node) =>
        node.Type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_IDisposable);

    public bool IsAsyncDisposable(DependencyNode node) =>
        node.Type.AllInterfaces.Any(i => IsAsyncDisposable(node.Binding.SemanticModel.Compilation, i));

    private static bool IsEnumerable(DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.Enumerable };

    private static bool IsAsyncEnumerable(DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.AsyncEnumerable };

    private bool IsAsyncDisposable(Compilation compilation, ISymbol type) =>
        types.TryGet(Core.SpecialType.IAsyncDisposable, compilation) is {} asyncDisposableType
        && types.TypeEquals(type, asyncDisposableType);

    private static bool IsDelegate(DependencyNode node) =>
        node.Type.TypeKind == TypeKind.Delegate;
}