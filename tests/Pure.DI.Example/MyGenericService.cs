// ReSharper disable UnusedMember.Global

namespace OtherAssembly;

class MyGenericService<T>(IMyDependency dependency, int id) : IMyGenericService<T>
{
    public int Id { get; } = id;

    public void DoSomething(T value) =>
        dependency.DoSomething();
}