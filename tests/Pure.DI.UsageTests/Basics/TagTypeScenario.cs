/*
$v=true
$p=6
$d=Tag Type
$h=`Tag.Type` in bindings replaces the expression `typeof(T)`, where `T` is the type of the implementation in a binding.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Basics.TagTypeScenario;

using Shouldly;
using Xunit;

// {
interface IDependency;

class AbcDependency : IDependency;
        
class XyzDependency : IDependency;
        
class Dependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
    
    IDependency Dependency3 { get; }
}

class Service(
    [Tag(typeof(AbcDependency))] IDependency dependency1,
    [Tag(typeof(XyzDependency))] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            // Tag.Type here is the same as typeof(AbcDependency) 
            .Bind<IDependency>(Tag.Type, default).To<AbcDependency>()
            // Tag.Type here is the same as typeof(XyzDependency)
            .Bind<IDependency>(Tag.Type).As(Lifetime.Singleton).To<XyzDependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root")
            // "XyzRoot" is root name, typeof(XyzDependency) is tag
            .Root<IDependency>("XyzRoot", typeof(XyzDependency));

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency1.ShouldBeOfType<AbcDependency>();
        service.Dependency2.ShouldBeOfType<XyzDependency>();
        service.Dependency2.ShouldBe(composition.XyzRoot);
        service.Dependency3.ShouldBeOfType<AbcDependency>();
// }            
        composition.SaveClassDiagram();
    }
}