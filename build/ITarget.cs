namespace Build;

interface ITarget<T>
{
    Task<T> RunAsync(CancellationToken cancellationToken);
}