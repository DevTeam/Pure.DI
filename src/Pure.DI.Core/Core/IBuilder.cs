namespace Pure.DI.Core;

interface IBuilder<in TData, out T>
{
    public T Build(TData data);
}