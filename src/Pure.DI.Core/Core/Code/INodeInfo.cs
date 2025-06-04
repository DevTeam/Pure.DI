namespace Pure.DI.Core.Code;

interface INodeInfo
{
    bool IsLazy(DependencyNode node);

    bool IsDisposableAny(DependencyNode node);

    bool IsDisposable(DependencyNode node);

    bool IsAsyncDisposable(DependencyNode node);
}