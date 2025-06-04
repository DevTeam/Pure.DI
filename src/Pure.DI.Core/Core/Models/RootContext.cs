namespace Pure.DI.Core.Models;

readonly record struct RootsContext(MdSetup Setup, ImmutableArray<Root> Roots);