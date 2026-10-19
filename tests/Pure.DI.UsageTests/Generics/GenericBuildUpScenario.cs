/*
$v=true
$p=10
$d=Build up of an existing generic object
$sa=Build up of an existing object
$sa=Generic builder
$h=In other words, injecting the necessary dependencies via methods, properties, or fields into an existing object. `ctx.BuildUp(context)` fills an instance created inside a factory binding, so an object whose constructor cannot express its dependencies still joins the graph.
$f=>[!NOTE]
$f=>Generic build-up allows you to inject dependencies into existing generic objects after their creation.
$f=
$f=Use it for generic types that a framework constructs - view models, request contexts, entities returned by an object mapper - while the dependencies they declare still come from the composition.
$f=The call runs inside the binding, so the effort is paid on every resolution of that binding, not once. Dependencies injected this way are not owned by the composition, which means an `IDisposable` member stays the caller's responsibility.
$f=>[!NOTE]
$f=>A `[Dependency]` member with no matching binding keeps its initial value instead of failing the build, so a missing injection shows up as a null or default value at run time.
$f=
$f=Prefer constructor injection for types the composition creates, and use build-up where the framework owns instantiation. See [Build up of an existing object](build-up-of-an-existing-object.md) for the non-generic case and [Generic builder](generic-builder.md) to expose build-up as a public method.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Generics.GenericBuildUpScenario;

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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .RootArg<string>("userName")
            .Bind().To(Guid.NewGuid)
            .Bind().To(ctx => {
                // The "BuildUp" method injects dependencies into an existing object.
                // This is useful when the object is created externally (e.g., by a UI framework
                // or an ORM) or requires specific initialization before injection.
                var context = new UserContext<TTS>();
                ctx.BuildUp(context);
                return context;
            })
            .Bind().To<Facade<TTS>>()

            // Composition root
            .Root<IFacade<Guid>>("GetFacade");

        var composition = new Composition();
        var facade = composition.GetFacade("Erik");

        facade.Context.UserName.ShouldBe("Erik");
        facade.Context.Id.ShouldNotBe(Guid.Empty);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IUserContext<out T>
    where T : struct
{
    string UserName { get; }

    T Id { get; }
}

class UserContext<T> : IUserContext<T>
    where T : struct
{
    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public string UserName { get; set; } = "";

    public T Id { get; private set; }

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public void SetId(T id) => Id = id;
}

interface IFacade<out T>
    where T : struct
{
    IUserContext<T> Context { get; }
}

record Facade<T>(IUserContext<T> Context)
    : IFacade<T> where T : struct;
// }