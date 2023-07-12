/*
$v=true
$p=0
$d=Func
$h=_Func<T>_ helps when the logic must enter instances of some type on demand or more than once. This is a very handy mechanism for instance replication. For example it is used when implementing the `Lazy<T>` injection.
$f=Be careful, replication takes into account the lifetime of the object. 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.BCL.FuncScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service : IService
{
    public Service(Func<IDependency> dependencyFactory) =>
        Dependencies = Enumerable
            .Range(0, 10)
            .Select(_ => dependencyFactory())
            .ToImmutableArray();

    public ImmutableArray<IDependency> Dependencies { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
        // FormatCode = On
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Length.ShouldBe(10);
// }            
        TestTools.SaveClassDiagram(new Composition(), nameof(FuncScenario));
    }
}