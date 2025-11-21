/*
$v=true
$p=6
$d=Tags
$h=Sometimes it's important to take control of building a dependency graph. For example, when there are different implementations of the same interface. In this case, _tags_ will help:
$f=The example shows how to:
$f=- Define multiple bindings for the same interface
$f=- Use tags to differentiate between implementations
$f=- Control lifetime management
$f=- Inject tagged dependencies into constructors
$f=
$f=The tag can be a constant, a type, a [smart tag](smart-tags.md), or a value of an `Enum` type. The _default_ and _null_ tags are also supported.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable PreferConcreteValueOverDefault
namespace Pure.DI.UsageTests.Basics.TagsScenario;

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
            // The `default` tag is used to resolve dependencies
            // when the tag was not specified by the consumer
            .Bind<IApiClient>("Public", default).To<RestApiClient>()
            .Bind<IApiClient>("Internal").As(Lifetime.Singleton).To<InternalApiClient>()
            .Bind<IApiFacade>().To<ApiFacade>()

            // "InternalRoot" is a root name, "Internal" is a tag
            .Root<IApiClient>("InternalRoot", "Internal")

            // Specifies to create the composition root named "Root"
            .Root<IApiFacade>("Api");

        var composition = new Composition();
        var api = composition.Api;
        api.PublicClient.ShouldBeOfType<RestApiClient>();
        api.InternalClient.ShouldBeOfType<InternalApiClient>();
        api.InternalClient.ShouldBe(composition.InternalRoot);
        api.DefaultClient.ShouldBeOfType<RestApiClient>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IApiClient;

class RestApiClient : IApiClient;

class InternalApiClient : IApiClient;

interface IApiFacade
{
    IApiClient PublicClient { get; }

    IApiClient InternalClient { get; }

    IApiClient DefaultClient { get; }
}

class ApiFacade(
    [Tag("Public")] IApiClient publicClient,
    [Tag("Internal")] IApiClient internalClient,
    IApiClient defaultClient)
    : IApiFacade
{
    public IApiClient PublicClient { get; } = publicClient;

    public IApiClient InternalClient { get; } = internalClient;

    public IApiClient DefaultClient { get; } = defaultClient;
}
// }