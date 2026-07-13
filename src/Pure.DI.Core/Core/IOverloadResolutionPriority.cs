namespace Pure.DI.Core;

interface IOverloadResolutionPriority
{
    int Get(Compilation compilation, IMethodSymbol method);
}
