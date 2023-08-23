// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal static class Formatting
{
    public const int IndentSize = 2;
    private const int IndentsCount = 5;
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

    private static string IndentInternal(int count = 1) => new(' ', count * IndentSize);
}