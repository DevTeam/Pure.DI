// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvocationIsSkipped
namespace Pure.DI.Core;

internal sealed class Log<T> : ILog<T>
{
    private readonly ISettings _settings;
    private readonly IFileSystem _fileSystem;

    public Log(
        ISettings settings,
        IFileSystem fileSystem)
    {
        _settings = settings;
        _fileSystem = fileSystem;
    }

    public void Trace(Func<string[]> messageFactory)
    {
        if (_settings.Verbosity >= Verbosity.Diagnostic)
        {
            Write("TRC", messageFactory);
        }
    }

    public void Info(Func<string[]> messageFactory)
    {
        if (_settings.Verbosity >= Verbosity.Normal)
        {
            Write("INF", messageFactory);
        }
    }

    public void Warning(params string[] warning)
    {
        if (_settings.Verbosity >= Verbosity.Minimal)
        {
            Write("WRN", () => warning);
        }

        Debug.Assert(true, string.Join(Environment.NewLine, warning));
    }

    public void Error(params string[] error)
    {
        Write("ERR", () => error);
        Debug.Assert(true, string.Join(Environment.NewLine, error));
    }

    private void Write(string category, Func<string[]> messageFactory)
    {
        if (_settings.TryGetLogFile(out var logFile))
        {
            _fileSystem.AppendFile(logFile, messageFactory().Select(i => $"{DateTime.Now:HH:mm:ss:fff} {category} {typeof(T).Name}: {i}"));
        }
    }
}