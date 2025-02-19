// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable ReturnTypeCanBeEnumerable.Global

namespace Pure.DI.Core;

using System.Buffers;

sealed class LinesBuilder : IEnumerable<string>
{
    private readonly List<Line> _lines = [];
    private readonly StringBuilder _sb = new();
    private Indent _indent;

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

    public IEnumerator<string> GetEnumerator()
    {
        FlushLines();
        return _lines.Select(i => $"{GetIndent(i.Indent)}{i.Text}").GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

    public IDisposable Indent(int size = 1)
    {
        IncIndent(size);
        return Disposables.Create(() => DecIndent(size));
    }

    public void IncIndent(int size = 1)
    {
        FlushLines();
        _indent = new Indent(_indent.Value + size);
    }

    public void DecIndent(int size = 1)
    {
        FlushLines();
        _indent = new Indent(_indent.Value - size);
    }

    public IDisposable SaveToArray(Encoding encoding, out byte[] buffer, out int size)
    {
        FlushLines();
        var charCount = 0;
        var newLine = Environment.NewLine;
        foreach (var line in _lines)
        {
            charCount += GetIndent(line.Indent).Length;
            charCount += line.Text.Length;
            charCount += newLine.Length;
        }

        size = encoding.GetMaxByteCount(charCount);
        var rent = ArrayPool<byte>.Shared.Rent(size);
        buffer = rent;
        var position = 0;
        foreach (var line in _lines)
        {
            var indent = GetIndent(line.Indent);
            position += encoding.GetBytes(indent, 0, indent.Length, buffer, position);
            position += encoding.GetBytes(line.Text, 0, line.Text.Length, buffer, position);
            position += encoding.GetBytes(newLine, 0, newLine.Length, buffer, position);
        }

        size = position;
        return Disposables.Create(() => ArrayPool<byte>.Shared.Return(rent));
    }

    private void FlushLines()
    {
        if (_sb.Length > 0)
        {
            AppendLine();
        }
    }

    private static string GetIndent(int indent) =>
        new Indent(indent).ToString();
}