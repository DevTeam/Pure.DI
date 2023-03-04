namespace Pure.DI.Core;

internal class Indent
{
    private readonly string _str;
    
    public Indent(int value)
    {
        Value = value;
        _str = Formatting.IndentPrefix(this);
    }

    public int Value { get; set; }

    public static implicit operator Indent(int value) => new(value);

    public override string ToString() => _str;
}