/*
$v=true
$p=7
$d=Build up of an existing generic object
$h=In other words, injecting the necessary dependencies via methods, properties, or fields into an existing object.
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .RootArg<string>("userName")
            .Bind().To(_ => Guid.NewGuid())
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