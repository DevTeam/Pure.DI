namespace Build.Targets;

internal interface ITarget<T>
{
    ValueTask<T> RunAsync(CancellationToken cancellationToken);
}