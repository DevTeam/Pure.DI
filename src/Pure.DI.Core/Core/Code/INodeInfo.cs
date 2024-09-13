namespace Pure.DI.Core.Code;

internal interface INodeInfo
{
    bool IsDelegate(DependencyNode node);

    bool IsLazy(DependencyNode node);

    bool IsDisposable(DependencyNode node);

    bool IsAsyncDisposable(DependencyNode node);
}