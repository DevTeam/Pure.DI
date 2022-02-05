namespace Pure.DI.Core;

internal readonly struct Source
{
    public readonly SourceType Type;
    public readonly string HintName;
    public readonly SourceText Code;

    public Source(SourceType type, string hintName, SourceText code)
    {
        Type = type;
        HintName = hintName;
        Code = code;
    }
}