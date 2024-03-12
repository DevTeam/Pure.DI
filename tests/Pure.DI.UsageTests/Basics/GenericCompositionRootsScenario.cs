/*
$v=true
$p=1
$d=Generic composition roots
$h=A generic composition root is represented by a method.
$f=When a generic composition root is used, `Resolve` methods cannot be used to resolve them.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedTypeParameter
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.GenericCompositionRootsScenario;

using Shouldly;
using Xunit;

// {
interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>;

class Service<T>(IDependency<T> dependency) : IService<T>;

class OtherService<T>(IDependency<T> dependency) : IService<T>;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            .Bind().To<Dependency<TT>>()
            .Bind().To<Service<TT>>()
            // Creates OtherService manually,
            // just for the sake of example
            .Bind("Other").To(ctx =>
            {
                ctx.Inject(out IDependency<TT> dependency);
                return new OtherService<TT>(dependency);
            })
            
            // Specifies to create a regular public method
            // to get a composition root of type Service<T>
            // with the name "GetMyRoot"
            .Root<IService<TT>>("GetMyRoot")
            
            // Specifies to create a regular public method
            // to get a composition root of type OtherService<T>
            // with the name "GetOtherService"
            // using the "Other" tag
            .Root<IService<TT>>("GetOtherService", "Other");

        var composition = new Composition();
        
        // service = new Service<int>(new Dependency<int>());
        var service = composition.GetMyRoot<int>();
        
        // someOtherService = new OtherService<int>(new Dependency<int>());
        var someOtherService = composition.GetOtherService<string>();
// }            
        service.ShouldBeOfType<Service<int>>();
        someOtherService.ShouldBeOfType<OtherService<string>>();
        composition.SaveClassDiagram();
    }
}