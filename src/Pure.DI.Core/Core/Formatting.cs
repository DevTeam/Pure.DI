// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

static class Formatting
{
    private const int IndentsCount = 64;
    private static readonly string[] Indents;

    static Formatting()
    {
        var indentsBuilder = ImmutableArray.CreateBuilder<string>(IndentsCount);
        for (var indentIndex = 0; indentIndex < IndentsCount; indentIndex++)
        {
            indentsBuilder.Add(IndentInternal(indentIndex));
        }

        Indents = indentsBuilder.ToArray();
    }

    public static string IndentPrefix(Indent indent) =>
        indent.Value switch
        {
            <= 0 => string.Empty,
            < IndentsCount => Indents[indent.Value],
            _ => IndentInternal(indent.Value)
        };

    private static string IndentInternal(int count = 1) =>
        string.Intern(new string('\t', count));
}