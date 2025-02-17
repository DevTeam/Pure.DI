namespace Pure.DI.Core;

interface IProcessingNode
{
    DependencyNode Node { get; }

    ISet<Injection> Contracts { get; }

    IReadOnlyCollection<InjectionInfo> Injections { get; }
}