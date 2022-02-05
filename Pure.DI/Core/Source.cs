namespace Pure.DI.Core;

internal readonly struct Source
{
    public readonly string HintName;
    public readonly SourceText Code;

    public Source(string hintName, SourceText code)
    {
        HintName = hintName;
        Code = code;
    }
}