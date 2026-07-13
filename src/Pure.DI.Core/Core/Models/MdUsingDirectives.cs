namespace Pure.DI.Core.Models;

readonly record struct MdUsingDirectives(
    in ImmutableArray<string> UsingDirectives,
    in ImmutableArray<string> StaticUsingDirectives,
    in ImmutableArray<(string name, string type)> Aliases)
{
    public MdUsingDirectives(
        IEnumerable<string> usingDirectives,
        IEnumerable<string> staticUsingDirectives,
        IEnumerable<(string name, string type)> aliases)
        :this(
            usingDirectives.Distinct().ToImmutableArray(),
            staticUsingDirectives.Distinct().ToImmutableArray(),
            aliases.ToImmutableArray())
    {
    }
}