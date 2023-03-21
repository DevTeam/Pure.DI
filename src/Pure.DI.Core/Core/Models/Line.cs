namespace Pure.DI.Core.Models;

internal readonly record struct Line(int Indent, string Text)
{
    public override string ToString() => $"{new Indent(Indent)}{Text}";
}