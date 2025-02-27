namespace Build.Core;

interface IInitializable
{
    Task InitializeAsync(CancellationToken cancellationToken);
}