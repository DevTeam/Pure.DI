namespace Pure.DI.Core.Models;

readonly record struct Line(int Indent, string Text)
{
    public override string ToString() => $"{new Indent(Indent)}{Text}";
}