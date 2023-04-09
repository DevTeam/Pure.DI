// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.DelegateAllocation
namespace Pure.DI.Core;

internal class Formatting : IFormatting
{
    public const int IndentSize = 2;
    private const int IndentsCount = 5;
    private static readonly ImmutableArray<string> Indents;

    static Formatting()
    {
        var indentsBuilder = ImmutableArray.CreateBuilder<string>(IndentsCount);
        for (var indentIndex = 0; indentIndex < IndentsCount; indentIndex++)
        {
            indentsBuilder.Add(IndentInternal(indentIndex));
        }

        Indents = indentsBuilder.ToImmutableArray();
    }
    
    public string Indent(Indent indent) => IndentPrefix(indent);

    public static string IndentPrefix(Indent indent) =>
        indent.Value switch
        {
            <= 0 => string.Empty,
            < IndentsCount => Indents[indent.Value],
            _ => IndentInternal(indent.Value)
        };

    private static string IndentInternal(int count = 1) => new(' ', count * IndentSize);
}