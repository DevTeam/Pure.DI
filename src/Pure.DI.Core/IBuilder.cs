namespace Pure.DI;

public interface IBuilder<in TData, out T>
{
    public T Build(TData data, CancellationToken cancellationToken);
}