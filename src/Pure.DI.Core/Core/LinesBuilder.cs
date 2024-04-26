// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable ReturnTypeCanBeEnumerable.Global
namespace Pure.DI.Core;

internal sealed class LinesBuilder: IEnumerable<string>
{
    private static readonly string[] Indents = new string[64]; 
    private readonly StringBuilder _sb = new(); 
    private readonly List<Line> _lines = [];
    private readonly Indent _indent;

    static LinesBuilder()
    {
        for (var i = 0; i < Indents.Length; i++)
        {
            Indents[i] = new Indent(i).ToString();
        }
    }

    public LinesBuilder(Indent indent) => _indent = new Indent(indent.Value - 1);

    public LinesBuilder() => _indent = new Indent(0);

    public int Count => _sb.Length > 0 ? 1 : 0 + _lines.Count;

    public IReadOnlyCollection<Line> Lines
    {
        get
        {
            FlushLines();
            return _lines;
        }
    }

    public void Append(string text) => _sb.Append(text);

    public void AppendLine(in Line line)
    {
        FlushLines();
        _lines.Add(line with { Indent = line.Indent + _indent.Value });
    }

    public void AppendLines(IEnumerable<Line> lines)
    {
        foreach (var line in lines)
        {
            AppendLine(line);
        }
    }

    public void AppendLine(string line = "")
    {
        if (_sb.Length > 0)
        {
            _sb.Append(line);
            _lines.Add(new Line(_indent.Value, _sb.ToString()));
            _sb.Clear();
        }
        else
        {
            _lines.Add(new Line(_indent.Value, line));
        }
    }

    public IDisposable Indent(int value = 1)
    {
        
        IncIndent(value);
        return Disposables.Create(() => DecIndent(value));
    }

    public void IncIndent(int value = 1)
    {
        FlushLines();
        _indent.Value += value;
    }

    public void DecIndent(int value = 1)
    {
        FlushLines();
        _indent.Value -= value;
    }

    public IEnumerator<string> GetEnumerator()
    {
        FlushLines();
        return _lines.Select(i => $"{GetIndent(i.Indent)}{i.Text}").GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void FlushLines()
    {
        if (_sb.Length > 0)
        {
            AppendLine();
        }
    }

    private static string GetIndent(int indent)
    {
        if (indent < 1)
        {
            return string.Empty;
        }

        if (indent < Indents.Length)
        {
            return Indents[indent];
        }

        return new Indent(indent).ToString();
    }
}