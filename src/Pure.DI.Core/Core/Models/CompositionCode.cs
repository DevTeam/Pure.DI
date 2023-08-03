namespace Pure.DI.Core.Models;

internal record CompositionCode(
    DependencyGraph Source,
    in CompositionName Name,
    in ImmutableArray<MdUsingDirectives> UsingDirectives,
    in ImmutableArray<Variable> Singletons,
    in ImmutableArray<Variable> Args,
    in ImmutableArray<Root> Roots,
    int DisposableSingletonsCount,
    LinesBuilder Code,
    int MembersCount);