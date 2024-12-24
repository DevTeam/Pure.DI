// ReSharper disable UnusedParameter.Global

namespace OtherAssembly;

public interface IMyGenericService<in T>
{
    public void DoSomething(T value);
}