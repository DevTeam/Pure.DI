// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable InvertIf
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core;

internal class Logger<T> : ILogger<T>, IDisposable
{
    private readonly IResourceManager _resourceManager;
    private readonly IFormatting _formatting;
    private readonly IClock _clock;
    private readonly IContextDiagnostic _diagnostic;
    private readonly string _name;
    private readonly DiagnosticSeverity _severity = DiagnosticSeverity.Warning;
    private readonly string _logFileName = "";
    private StreamWriter? _logWriter;

    public Logger(
        IContextOptions contextOptions,
        IResourceManager resourceManager,
        IFormatting formatting,
        IClock clock,
        IContextDiagnostic diagnostic)
    {
        _resourceManager = resourceManager;
        _formatting = formatting;
        _clock = clock;
        _diagnostic = diagnostic;
        _name = typeof(T).Name;
        try
        {
            if (contextOptions.GlobalOptions.TryGetValue(GlobalSettings.Severity, out var severityStr)
                && !string.IsNullOrWhiteSpace(severityStr)
                && Enum.TryParse<DiagnosticSeverity>(severityStr, true, out var severityVal))
            {
                _severity = severityVal;
            }
            else
            {
                _severity = DiagnosticSeverity.Warning;
            }

            if (contextOptions.GlobalOptions.TryGetValue(GlobalSettings.LogFile, out var logFileName)
                && !string.IsNullOrWhiteSpace(logFileName))
            {
                var logDirectory = Path.GetDirectoryName(logFileName);
                if (logDirectory is not null)
                {
                    Directory.CreateDirectory(logDirectory);
                }

                _logFileName = logFileName;
            }
        }
        catch
        {
            // ignored
        }
    }

    public bool IsEnabled(DiagnosticSeverity severity) => true;

    public void Log(in LogEntry logEntry)
    {
        if (logEntry.Severity < _severity)
        {
            return;
        }
        
        var sb = new StringBuilder();
        var severityCode = logEntry.Severity switch
        {
            DiagnosticSeverity.Error => "ERR",
            DiagnosticSeverity.Warning => "WRN",
            DiagnosticSeverity.Info => "INF",
            DiagnosticSeverity.Hidden => "TRC",
            _ => throw new ArgumentOutOfRangeException(nameof(logEntry.Severity), logEntry.Severity, null)
        };
        sb.Append(severityCode);
        sb.Append(_clock.Now.ToString(" mm:ss.fff "));
        sb.Append(_name.PadRight(32, '.'));

        using var linesEnumerator = logEntry.Lines.GetEnumerator();
        var firstLine = string.Empty;
        if (linesEnumerator.MoveNext())
        {
            firstLine = linesEnumerator.Current ?? string.Empty;
            sb.Append(' ');
            sb.Append(firstLine);
        }

        if (!string.IsNullOrWhiteSpace(logEntry.Id))
        {
            var descriptor = new DiagnosticDescriptor(logEntry.Id, severityCode, firstLine, severityCode, logEntry.Severity, true);
            _diagnostic.ReportDiagnostic(Diagnostic.Create(descriptor, logEntry.Location));
        }
        
        WriteLines(Format(logEntry, sb.ToString(), linesEnumerator));
    }

    private IEnumerable<string> Format(LogEntry logEntry, string header, IEnumerator<string> linesEnumerator)
    {
        yield return header;
        while (linesEnumerator.MoveNext())
        {
            var line = linesEnumerator.Current;
            if (line != default)
            {
                yield return line;
            }
        }
        
        if (logEntry.Exception is {} exception)
        {
            foreach (var line in Format(exception.Message, Environment.NewLine, 2))
            {
                yield return line;
            }
        }

        if (logEntry.Location is {} location)
        {
            foreach (var line in Format(location, 1))
            {
                yield return line;
            }
        }
    }

    public void Dispose()
    {
        _logWriter?.Dispose();
        _logWriter = default;
    }

    private IEnumerable<string> Format(string lines, string postFix, int indent = 1, bool formatFirstLine = false, char separator = '\n')
    {
        var indentStr = _formatting.Indent(indent);
        var isFirst = true;
        var sb = new StringBuilder();
        foreach (var line in lines.Split(separator))
        {
            if (!isFirst)
            {
                sb.Append(postFix);
            }

            if (!isFirst || formatFirstLine)
            {
                for (var i = 0; i < indent; i++)
                {
                    sb.Append(indentStr);
                }
            }

            isFirst = false;
            sb.Append(line.TrimEnd());
            yield return sb.ToString();
            sb.Clear();
        }
    }
    
    private IEnumerable<string> Format(Location? location, int indent = 0)
    {
        if (location == default || !location.IsInSource)
        {
            yield break;
        }

        var source = location.SourceTree.ToString().Split(Environment.NewLine).ToArray();
        var lineSpan= location.GetLineSpan();
        var startLine = lineSpan.StartLinePosition.Line;
        var endLine = lineSpan.EndLinePosition.Line;
        
        yield return 
            startLine == endLine
                ? $"At line {(startLine + 1).ToString()}"
                : $"At lines {(startLine + 1).ToString()}-{(endLine + 1).ToString()}";
        
        var visualStartLine = startLine - 5;
        if (visualStartLine < 0)
        {
            visualStartLine = 0;
        }
        
        var visualEndLine = endLine + 10;
        if (visualEndLine >= source.Length)
        {
            visualEndLine = source.Length - 1;
        }
        
        var indentStr = _formatting.Indent(indent);

        var visualSourceLines = source
            .Skip(visualStartLine)
            .Take(visualEndLine + 1 - visualStartLine)
            .ToArray();

        if (visualSourceLines.Length == 0)
        {
            yield break;
        }
        
        var minPrefixSize = visualSourceLines.Where(i => !string.IsNullOrEmpty(i)).DefaultIfEmpty(string.Empty).Select(GetPrefixSize).Min();
        IEnumerable<string> sourceLines = visualSourceLines;
        if (minPrefixSize > 0)
        {
            sourceLines = sourceLines.Select(i => minPrefixSize < i.Length ? i[minPrefixSize..] : i);
        }

        var hasLineSelector = visualStartLine > 0 || visualEndLine < visualSourceLines.Length - 1; 
        var digits = (int)(1 + Math.Log10(visualSourceLines.Length));
        sourceLines = sourceLines
            .Select((line, number) => $"{indentStr}{(number + 1 + visualStartLine).ToString().PadRight(digits)}{(!hasLineSelector ? "" : number + visualStartLine >= startLine && number + visualStartLine <= endLine ? "->" : "  ").ToString()} {line}");

        foreach (var line in sourceLines)
        {
            yield return line;
        }
    }

    private static int GetPrefixSize(IEnumerable<char> items) => items.TakeWhile(i => i == ' ').Count();

    private void WriteLines(IEnumerable<string> lines)
    {
        if (!TryGetWriter(out var writer))
        {
            return;
        }

        foreach (var line in lines)
        {
            writer.WriteLine(line);
        }
    }
    
    private bool TryGetWriter([NotNullWhen(true)] out StreamWriter? writer)
    {
        if (_logWriter is {} )
        {
            writer = _logWriter;
            return true;
        }

        if (string.IsNullOrEmpty(_logFileName))
        {
            writer = default;
            return false;
        }
        
        var logFile = File.Open(_logFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _logWriter = new StreamWriter(logFile);
        _resourceManager.Register(Disposables.Create(() =>
        {
            _logWriter.Flush();
            _logWriter.Dispose();
            _logWriter = default;
        }));

        writer = _logWriter;
        return true;
    }
}