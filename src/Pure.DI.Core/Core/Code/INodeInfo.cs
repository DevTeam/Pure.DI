namespace Pure.DI.Core.Code;

interface INodeInfo
{
    bool IsDelegate(DependencyNode node);

    bool IsLazy(DependencyNode node);

    bool IsDisposableAny(DependencyNode node);

    bool IsDisposable(DependencyNode node);

    bool IsAsyncDisposable(DependencyNode node);
}