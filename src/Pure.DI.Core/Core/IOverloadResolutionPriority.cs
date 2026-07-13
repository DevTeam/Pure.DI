namespace Pure.DI.Core;

interface IOverloadResolutionPriority
{
    int Get(Compilation compilation, IMethodSymbol method);

    bool TryGet(Compilation compilation, IMethodSymbol method, out int priority);
}
