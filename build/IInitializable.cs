namespace Build;

interface IInitializable
{
    Task InitializeAsync(CancellationToken cancellationToken);
}