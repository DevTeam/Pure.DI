namespace Pure.DI.Core;

internal interface IGraphPath
{
    bool TryAddPart(in DependencyNode node);
    bool IsCompleted(in DependencyNode node);
}