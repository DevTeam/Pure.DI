namespace Pure.DI.Core;

internal readonly struct Indent(int value)
{
    public int Value { get; } = value;

    public static implicit operator Indent(int value) => new(value);

    public override string ToString() => Formatting.IndentPrefix(this);
}