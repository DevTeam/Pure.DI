namespace _PureDIProjectName_;

public interface IOutput
{
    Task WriteLineAsync(string line = "", CancellationToken cancellationToken = default);
}