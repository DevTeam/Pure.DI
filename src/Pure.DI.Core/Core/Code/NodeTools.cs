// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

using static Lifetime;
using SpecialType=Microsoft.CodeAnalysis.SpecialType;

sealed class NodeTools(
    ITypes types,
    ICache<NodeTools.LazyKey, bool> isLazy,
    ITypeSymbolComparer typeSymbolComparer) : INodeTools
{
    public bool IsLazy(DependencyNode node, DependencyGraph graph)
    {
        var key = new LazyKey(node, graph.Source.SemanticModel);
        if (isLazy.TryGet(key, out var result))
        {
            return result;
        }

        result = (IsDelegate(node) || IsEnumerable(node) || IsAsyncEnumerable(node))
                 && (node.Factory is not {} factory || IsLazyFactory(factory, key.SemanticModel));
        isLazy.Set(key, result);
        return result;
    }

    public bool IsBlock(IDependencyNode node) =>
        node.ActualLifetime is Singleton or Scoped or PerResolve;

    public bool IsDisposableAny(DependencyNode node) =>
        node.Type.AllInterfaces.Any(i =>
            i.SpecialType == SpecialType.System_IDisposable
            || IsAsyncDisposable(node.Binding.SemanticModel.Compilation, i));

    public bool IsDisposable(DependencyNode node) =>
        node.Type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_IDisposable);

    public bool IsAsyncDisposable(DependencyNode node) =>
        node.Type.AllInterfaces.Any(i => IsAsyncDisposable(node.Binding.SemanticModel.Compilation, i));

    public int EstimateBodyCost(IDependencyNode node, bool isLockRequired)
    {
        var concrete = node.Node;
        var cost = 0;

        if (concrete.Implementation is { } implementation)
        {
            cost += implementation.Constructor.Parameters.Length;
            cost += implementation.Properties.Length;
            cost += implementation.Fields.Length;
            cost += implementation.Methods.Length;
        }

        if (concrete.Factory is not null)
        {
            cost += 6;
        }

        if (concrete.Construct is not null)
        {
            cost += 2;
        }

        if (isLockRequired)
        {
            cost += 4;
        }

        if (IsDisposableAny(concrete))
        {
            cost += 1;
        }

        return cost;
    }

    private static bool IsEnumerable(DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.Enumerable };

    private static bool IsAsyncEnumerable(DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.AsyncEnumerable };

    private bool IsAsyncDisposable(Compilation compilation, ISymbol type) =>
        types.TryGet(Core.SpecialType.IAsyncDisposable, compilation) is {} asyncDisposableType
        && types.TypeEquals(type, asyncDisposableType);

    private static bool IsDelegate(DependencyNode node) =>
        node.Type.TypeKind == TypeKind.Delegate;

    private bool IsLazyFactory(DpFactory factory, SemanticModel semanticModel) =>
        factory.Resolvers.All(i => IsLazy(factory, i.Source.Source, semanticModel))
        && factory.Initializers.All(i => IsLazy(factory, i.Source.Source, semanticModel))
        && factory.OverridesMap.Values.All(i => IsLazy(factory, i.Source.Source, semanticModel));

    private bool IsLazy(DpFactory factory, ExpressionSyntax source, SemanticModel semanticModel)
    {
        if (semanticModel.SyntaxTree != factory.Source.Factory.SyntaxTree)
        {
            return true;
        }

        var factoryTypeInfo = semanticModel.GetTypeInfo(factory.Source.Factory);
        var factoryType = factoryTypeInfo.Type ?? factoryTypeInfo.ConvertedType;
        if (factoryType is null)
        {
            return false;
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var node in source.Ancestors().TakeWhile(i => i != source))
        {
            if (semanticModel.SyntaxTree != node.SyntaxTree)
            {
                continue;
            }

            var invocationTypeInfo = semanticModel.GetTypeInfo(node);
            var invocationType = invocationTypeInfo.Type ?? invocationTypeInfo.ConvertedType;
            if (invocationType is null)
            {
                continue;
            }

            return !typeSymbolComparer.RuntimeEquals(factoryType, invocationType);
        }

        return false;
    }

    internal readonly struct LazyKey(DependencyNode node, SemanticModel semanticModel) : IEquatable<LazyKey>
    {
        public readonly DependencyNode Node = node;
        private readonly int _bindingId = node.BindingId;
        public readonly SemanticModel SemanticModel = semanticModel;

        public bool Equals(LazyKey other) => _bindingId == other._bindingId && SemanticModel.Equals(other.SemanticModel);

        public override bool Equals(object? obj) => obj is LazyKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return _bindingId * 397 ^ SemanticModel.GetHashCode();
            }
        }
    }
}
