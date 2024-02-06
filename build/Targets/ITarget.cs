namespace Build.Targets;

internal interface ITarget<T>
{
    Task<T> RunAsync(CancellationToken cancellationToken);
}