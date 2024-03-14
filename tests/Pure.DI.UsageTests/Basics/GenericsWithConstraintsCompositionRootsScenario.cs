/*
$v=true
$p=4
$d=Generic with constraints composition roots
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedTypeParameter
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario;

using Shouldly;
using Xunit;

// {
interface IDependency<T>
    where T: IDisposable;

class Dependency<T> : IDependency<T>
    where T: IDisposable;

interface IService<T, TStruct>
    where T: IDisposable
    where TStruct: struct;

class Service<T, TStruct>(IDependency<T> dependency) : IService<T, TStruct>
    where T: IDisposable
    where TStruct: struct;

class OtherService<T, TStruct>(IDependency<T> dependency) : IService<T, TStruct>
    where T: IDisposable
    where TStruct: struct;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            .Bind().To<Dependency<TTDisposable>>()
            .Bind().To<Service<TTDisposable, TTS>>()
            // Creates OtherService manually,
            // just for the sake of example
            .Bind("Other").To(ctx =>
            {
                ctx.Inject(out IDependency<TTDisposable> dependency);
                return new OtherService<TTDisposable, TTS>(dependency);
            })
            
            // Specifies to create a regular public method
            // to get a composition root of type Service<T>
            // with the name "GetMyRoot"
            .Root<IService<TTDisposable, TTS>>("GetMyRoot")
            
            // Specifies to create a regular public method
            // to get a composition root of type OtherService<T>
            // with the name "GetOtherService"
            // using the "Other" tag
            .Root<IService<TTDisposable, TTS>>("GetOtherService", "Other");

        var composition = new Composition();
        
        // service = new Service<int>(new Dependency<int>());
        var service = composition.GetMyRoot<Stream, double>();
        
        // someOtherService = new OtherService<int>(new Dependency<int>());
        var someOtherService = composition.GetOtherService<Stream, DateTime>();
// }            
        service.ShouldBeOfType<Service<Stream, double>>();
        someOtherService.ShouldBeOfType<OtherService<Stream, DateTime>>();
        composition.SaveClassDiagram();
    }
}