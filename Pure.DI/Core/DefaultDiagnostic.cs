namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class DefaultDiagnostic: IDiagnostic
    {
        private readonly IStdOut _stdOut;
        private readonly IStdErr _stdErr;

        public DefaultDiagnostic(IStdOut stdOut, IStdErr stdErr)
        {
            _stdOut = stdOut;
            _stdErr = stdErr;
        }

        public void Error(string id, string message, Location? location)
        {
            _stdErr.WriteErrorLine($"Error {id}: {message}{GetLine(location)}");
            throw new HandledException(message);
        }

        public void Warning(string id, string message, Location? location) =>
            _stdOut.WriteLine($"Warning {id}: {message}{GetLine(location)}");

        private static string GetLine(Location? location)
        {
            if (location == null || !location.IsInSource)
            {
                return string.Empty;
            }

            var line = location.SourceTree.ToString().Substring(location.SourceSpan.Start, location.SourceSpan.Length);
            return $" at line {location.GetMappedLineSpan().StartLinePosition.Line + 1}: {line}";
        }
    }
}
