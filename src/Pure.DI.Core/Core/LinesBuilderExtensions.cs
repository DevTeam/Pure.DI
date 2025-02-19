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
        return Disposables.Create(
            linesBuilder.Indent(),
            Disposables.Create(() => linesBuilder.AppendLine(BlockFinish)));
    }
}