namespace Pure.DI.Core;

using System.Runtime.CompilerServices;

static class LinesExtensions
{
    public const string BlockStart = "{";
    public const string BlockFinish = "}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDisposable CreateBlock(this Lines lines)
    {
        lines.AppendLine(BlockStart);
        lines.IncIndent();
        return Disposables.Create(() => {
            lines.DecIndent();
            lines.AppendLine(BlockFinish);
        });
    }
}