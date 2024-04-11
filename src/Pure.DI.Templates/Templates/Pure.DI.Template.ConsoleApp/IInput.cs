namespace _PureDIProjectName_;

internal interface IInput
{
    ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken = default);
}