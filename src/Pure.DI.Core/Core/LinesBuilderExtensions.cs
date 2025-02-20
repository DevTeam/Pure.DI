namespace Pure.DI.Core;

using System.Runtime.CompilerServices;

static class LinesBuilderExtensions
{
    public const string BlockStart = "{";
    public const string BlockFinish = "}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDisposable CreateBlock(this LinesBuilder linesBuilder)
    {
        linesBuilder.AppendLine(BlockStart);
        linesBuilder.IncIndent();
        return Disposables.Create(() => {
            linesBuilder.DecIndent();
            linesBuilder.AppendLine(BlockFinish);
        });
    }
}