// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable ReturnTypeCanBeEnumerable.Global

namespace Pure.DI.Core;

using System.Buffers;
using System.Runtime.CompilerServices;

sealed class Lines: IEnumerable<Line>
{
    private readonly List<Line> _lines = [];
    private readonly StringBuilder _sb = new();
    private Indent _indent;

    public Lines(Indent indent) => _indent = new Indent(indent.Value - 1);

    public Lines() => _indent = new Indent(0);

    public int Count =>
        _sb.Length > 0
            ? 1 + _lines.Count
            : _lines.Count;

    public IEnumerator<Line> GetEnumerator()
    {
        FlushLines();
        return _lines.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string text) => _sb.Append(text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine(in Line line)
    {
        FlushLines();
        _lines.Add(CreateLine(line));
    }

    public void AppendLines(IEnumerable<Line> lines)
    {
        FlushLines();
        foreach (var line in lines)
        {
            _lines.Add(CreateLine(line));
        }
    }

    public void AppendLines(Lines lines)
    {
        lines.FlushLines();
        FlushLines();
        foreach (var line in lines._lines)
        {
            _lines.Add(CreateLine(line));
        }
    }

    public void AppendLine(string line = "")
    {
        if (_sb.Length > 0)
        {
            _sb.Append(line);
            line = _sb.ToString();
            _sb.Clear();
        }

        _lines.Add(new Line(_indent.Value, line));
    }

    public IDisposable Indent(int size = 1)
    {
        IncIndent(size);
        return Disposables.Create(() => DecIndent(size));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncIndent(int size = 1) => _indent = new Indent(_indent.Value + size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DecIndent(int size = 1) => _indent = new Indent(_indent.Value - size);

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

    public override string ToString()
    {
        using var rent = SaveToArray(Encoding.UTF8, out var buffer, out var size);
        return Encoding.UTF8.GetString(buffer, 0, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushLines()
    {
        if (_sb.Length > 0)
        {
            AppendLine();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetIndent(int indent) =>
        new Indent(indent).ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Line CreateLine(Line line) => line with { Indent = line.Indent + _indent.Value };
}