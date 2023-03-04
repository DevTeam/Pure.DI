namespace Pure.DI.Core;

using System.Collections;
using System.Text;

internal class LinesBuilder: IEnumerable<string>
{
    private readonly StringBuilder _sb = new(); 
    private readonly List<Line> _lines = new();
    private readonly Indent _indent;

    public LinesBuilder(Indent indent) => _indent = new Indent(indent.Value - 1);

    public LinesBuilder() => _indent = new Indent(0);

    public Indent CurrentIndent => _indent.Value;
    
    public IEnumerable<Line> Lines
    {
        get
        {
            if (_sb.Length > 0)
            {
                AppendLine();
            }

            return _lines;
        }
    }

    public void Append(string text) => _sb.Append(text);

    public void AppendLine(in Line line) => 
        _lines.Add(line with { Indent = line.Indent + CurrentIndent.Value });

    public void AppendLines(in ImmutableArray<Line> lines)
    {
        foreach (var line in lines)
        {
            AppendLine(line);
        }
    }
    
    public void AppendLine(string line = "")
    {
        _sb.Append(line);
        _lines.Add(new Line(_indent.Value, _sb.ToString()));
        _sb.Clear();
    }

    public IDisposable Indent()
    {
        _indent.Value++;
        return Disposables.Create(() => _indent.Value--);
    }
    
    public void IncIndent() => _indent.Value++;
    
    public void DecIndent() => _indent.Value--;

    public IEnumerator<string> GetEnumerator()
    {
        if (_sb.Length > 0)
        {
            AppendLine();
        }

        return _lines.Select(i => $"{new Indent(i.Indent)}{i.Text}").GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}