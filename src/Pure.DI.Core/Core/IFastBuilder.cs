namespace Pure.DI.Core;

interface IFastBuilder<TData, out T>
{
    public T Build(in TData data);
}