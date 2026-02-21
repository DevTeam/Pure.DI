// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

using static Lifetime;
using SpecialType=Microsoft.CodeAnalysis.SpecialType;

sealed class NodeTools(ITypes types) : INodeTools
{
    public bool IsLazy(DependencyNode node, DependencyGraph graph) =>
        (IsDelegate(node) || IsEnumerable(node) || IsAsyncEnumerable(node)) && (node.Factory is not {} factory || IsLazyFactory(factory, graph.Source.SemanticModel));

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

    private static bool IsEnumerable(DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.Enumerable };

    private static bool IsAsyncEnumerable(DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.AsyncEnumerable };

    private bool IsAsyncDisposable(Compilation compilation, ISymbol type) =>
        types.TryGet(Core.SpecialType.IAsyncDisposable, compilation) is {} asyncDisposableType
        && types.TypeEquals(type, asyncDisposableType);

    private static bool IsDelegate(DependencyNode node) =>
        node.Type.TypeKind == TypeKind.Delegate;

    private static bool IsLazyFactory(DpFactory factory, SemanticModel semanticModel) =>
        factory.Resolvers.All(i => IsLazy(factory, i.Source.Source, semanticModel))
        && factory.Initializers.All(i => IsLazy(factory, i.Source.Source, semanticModel))
        && factory.OverridesMap.Values.All(i => IsLazy(factory, i.Source.Source, semanticModel));

    private static bool IsLazy(DpFactory factory, ExpressionSyntax source, SemanticModel semanticModel)
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

            return !SymbolEqualityComparer.Default.Equals(factoryType, invocationType);
        }

        return false;
    }
}