namespace _PureDIProjectName_;

public interface IInput
{
    ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken = default);
}