namespace OtherAssembly;

class MyService(IMyDependency dependency) : IMyService
{
    public void DoSomething() =>
        dependency.DoSomething();
}