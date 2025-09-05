namespace Pure.DI.Core.Code;

interface INodeTools
{
    bool IsLazy(DependencyNode node);

    bool IsBlock(IDependencyNode node);

    bool IsDisposableAny(DependencyNode node);

    bool IsDisposable(DependencyNode node);

    bool IsAsyncDisposable(DependencyNode node);
}