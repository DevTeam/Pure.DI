namespace Pure.DI.InterfaceGeneration;

sealed class CodeBuilder
{
    private readonly StringBuilder _sb = new();
    private int _indent;
    private string _currentIndent = string.Empty;

    public void Indent()
    {
        _indent += 4;
        _currentIndent = new string(' ', _indent);
    }

    public void Dedent()
    {
        _indent -= 4;
        _currentIndent = new string(' ', _indent);
    }

    public void BreakLine() => _sb.AppendLine();

    public void AppendIndented(string str)
    {
        _sb.Append(' ', _indent);
        _sb.Append(str);
    }

    public void AppendLine(string str)
    {
        _sb.Append(' ', _indent);
        _sb.AppendLine(str);
    }

    public void Append(string str) => _sb.Append(str);

    public void AppendAndNormalizeMultipleLines(string doc)
    {
        if (string.IsNullOrWhiteSpace(doc))
        {
            return;
        }

        foreach (var line in doc.Split([Environment.NewLine], StringSplitOptions.None))
        {
            _sb.AppendLine(IndentStr(line));
        }
    }

    private string IndentStr(string str) => str.TrimStart().Insert(0, _currentIndent);

    public string Build() => _sb.ToString();
}