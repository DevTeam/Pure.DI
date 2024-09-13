// ReSharper disable UnusedParameter.Global

namespace Pure.DI.Integration;

public interface IMyGenericService<in T>
{
    public void DoSomething(T value);
}