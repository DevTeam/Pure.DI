namespace Pure.DI.Core.Models;

readonly record struct MdUsingDirectives(
    in ImmutableArray<string> UsingDirectives,
    in ImmutableArray<string> StaticUsingDirectives);