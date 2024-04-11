namespace _PureDIProjectName_;

internal interface IOutput
{
    Task WriteLineAsync(string line = "", CancellationToken cancellationToken = default);
}