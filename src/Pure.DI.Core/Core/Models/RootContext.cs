namespace Pure.DI.Core.Models;

readonly record struct RootContext(MdSetup Setup, ImmutableArray<Root> Roots);