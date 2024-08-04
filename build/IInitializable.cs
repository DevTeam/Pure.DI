namespace Build;

internal interface IInitializable
{
    Task InitializeAsync(CancellationToken cancellationToken);
}