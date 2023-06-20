/*
$v=true
$p=100
$d=Overriding the BCL binding
$h=At any time, the default binding to the BCL type can be changed to your own:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests.BCL.OverridingBclBindingScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class AbcDependency : IDependency { }

internal class XyzDependency : IDependency { }

internal interface IService
{
    IDependency[] Dependencies { get; }
}

internal class Service : IService
{
    public Service(IDependency[] dependencies)
    {
        Dependencies = dependencies;
    }

    public IDependency[] Dependencies { get; }
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
            .Bind<IDependency[]>().To(_ => new IDependency[]
            {
                new AbcDependency(),
                new XyzDependency(),
                new AbcDependency()
            })
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Length.ShouldBe(3);
        service.Dependencies[0].ShouldBeOfType<AbcDependency>();
        service.Dependencies[1].ShouldBeOfType<XyzDependency>();
        service.Dependencies[2].ShouldBeOfType<AbcDependency>();
// }            
        TestTools.SaveClassDiagram(new Composition(), nameof(OverridingBclBindingScenario));
    }
}