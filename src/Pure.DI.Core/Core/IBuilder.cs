namespace Pure.DI.Core;

internal interface IBuilder<in TData, out T>
{
    public T Build(TData data, CancellationToken cancellationToken);
}