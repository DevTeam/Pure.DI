/*
$v=true
$p=5
$d=Generic async composition roots with constraints
$h=>[!IMPORTANT]
$h=>`Resolve' methods cannot be used to resolve generic composition roots.
$f=>[!IMPORTANT]
$f=>The method `Inject()`cannot be used outside of the binding setup.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedTypeParameter
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Generics.GenericAsyncCompositionRootsWithConstraintsScenario;

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

class OtherService<T>(IDependency<T> dependency) : IService<T, bool>
    where T: IDisposable;
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
// {            
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind().To<Dependency<TTDisposable>>()
            .Bind().To<Service<TTDisposable, TTS>>()
            // Creates OtherService manually,
            // just for the sake of example
            .Bind("Other").To(ctx =>
            {
                ctx.Inject(out IDependency<TTDisposable> dependency);
                return new OtherService<TTDisposable>(dependency);
            })
            
            // Specifies to use CancellationToken from the argument
            // when resolving a composition root
            .RootArg<CancellationToken>("cancellationToken")
            
            // Specifies to create a regular public method
            // to get a composition root of type Task<Service<T, TStruct>>
            // with the name "GetMyRootAsync"
            .Root<Task<IService<TTDisposable, TTS>>>("GetMyRootAsync")
            
            // Specifies to create a regular public method
            // to get a composition root of type Task<OtherService<T>>
            // with the name "GetOtherServiceAsync"
            // using the "Other" tag
            .Root<Task<IService<TTDisposable, bool>>>("GetOtherServiceAsync", "Other");

        var composition = new Composition();
        
        // Resolves composition roots asynchronously
        var service = await composition.GetMyRootAsync<Stream, double>(CancellationToken.None);
        var someOtherService = await composition.GetOtherServiceAsync<BinaryReader>(CancellationToken.None);
// }            
        service.ShouldBeOfType<Service<Stream, double>>();
        someOtherService.ShouldBeOfType<OtherService<BinaryReader>>();
        composition.SaveClassDiagram();
    }
}