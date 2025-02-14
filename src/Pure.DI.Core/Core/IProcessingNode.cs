namespace Pure.DI.Core;

internal interface IProcessingNode
{
    DependencyNode Node { get; }

    ISet<Injection> Contracts { get; }

    IReadOnlyCollection<InjectionInfo> Injections { get; }
}