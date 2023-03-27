namespace Pure.DI.Core.Models;

internal readonly record struct MdUsingDirectives(
    in ImmutableArray<string> UsingDirectives,
    in ImmutableArray<string> StaticUsingDirectives);