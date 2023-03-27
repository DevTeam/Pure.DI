namespace Pure.DI.Core.Models;

internal record CompositionCode(
    string ClassName,
    string Namespace,
    in ImmutableArray<MdUsingDirectives> UsingDirectives,
    in ImmutableArray<Field> Singletons,
    in ImmutableArray<Field> Args,
    in ImmutableArray<Root> Roots,
    int DisposableSingletonsCount,
    LinesBuilder Code,
    int MembersCount);