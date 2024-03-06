namespace Build;

internal interface ITarget<T>
{
    Task<T> RunAsync(CancellationToken cancellationToken);
}