namespace Pure.DI.Core.Models;

internal readonly record struct RootContext(MdSetup Setup, ImmutableArray<Root> Roots);