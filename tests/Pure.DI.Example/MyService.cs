namespace OtherAssembly;

internal class MyService(IMyDependency dependency) : IMyService
{
    public void DoSomething() =>
        dependency.DoSomething();
}