namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal class DefaultDiagnostic : IDiagnostic
{
    private readonly IStdOut _stdOut;
    private readonly IStdErr _stdErr;

    public DefaultDiagnostic(IStdOut stdOut, IStdErr stdErr)
    {
        _stdOut = stdOut;
        _stdErr = stdErr;
    }

    public void Error(IEnumerable<CodeError> errors)
    {
        var curErrors = errors.ToList(); 
        foreach (var error in curErrors)
        {
            _stdErr.WriteErrorLine($"Error {error.Id}: {error.Description}{GetLine(error.Locations)}");
        }
        
        throw new HandledException(string.Join(", ", curErrors.Select(i => i.Description)));
    }

    public void Error(string id, string message, params Location[] locations)
    {
        _stdErr.WriteErrorLine($"Error {id}: {message}{GetLine(locations)}");
        throw new HandledException(message);
    }

    public void Warning(string id, string message, params Location[] locations) =>
        _stdOut.WriteLine($"Warning {id}: {message}{GetLine(locations)}");

    public void Information(string id, string message, params Location[] locations)
    { }

    private static string GetLine(params Location[] locations) => 
        string.Join(Environment.NewLine, GetLines(locations));

    private static IEnumerable<string> GetLines(params Location[] locations) =>
        from location in locations.Where(i => i.IsInSource)
        let line = location.SourceTree?.ToString().Substring(location.SourceSpan.Start, location.SourceSpan.Length)
        where string.IsNullOrWhiteSpace(line)
        select $" at line {location.GetMappedLineSpan().StartLinePosition.Line + 1}: {line}";
}