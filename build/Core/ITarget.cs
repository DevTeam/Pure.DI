namespace Build.Core;

interface ITarget<T>
{
    Task<T> RunAsync(CancellationToken cancellationToken);
}