/*
$v=true
$p=10
$d=Generic roots
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Generics.GenericsRootsScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind().To<Dependency<TT>>()
            .Bind().To<Service<TT>>()
            // Creates OtherService manually,
            // just for the sake of example
            .Bind<OtherService<TT>>().To(ctx =>
            {
                ctx.Inject(out IDependency<TT> dependency);
                return new OtherService<TT>(dependency);
            })

            // Specifies to define composition roots for all types inherited from IService<TT>
            // available at compile time at the point where the method is called
            .Roots<IService<TT>>("GetMy{type}");

        var composition = new Composition();

        // service = new Service<int>(new Dependency<int>());
        var service = composition.GetMyService_T<int>();

        // someOtherService = new OtherService<int>(new Dependency<int>());
        var someOtherService = composition.GetMyOtherService_T<string>();
// }
        service.ShouldBeOfType<Service<int>>();
        someOtherService.ShouldBeOfType<OtherService<string>>();
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>;

class Service<T>(IDependency<T> dependency) : IService<T>;

class OtherService<T>(IDependency<T> dependency) : IService<T>;
// }