// ReSharper disable once CheckNamespace
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedMember.Local
namespace Pure.DI.UsageTests.IntegrationTests.MicrosoftDependencyInjectionTests;

using Microsoft.Extensions.DependencyInjection;
using Pure.DI;
using Pure.DI.MS;

public class MicrosoftDependencyInjectionTests
{
    [Fact]
    public void ShouldNotFallbackToUntaggedServiceForTaggedExternalDependency()
    {
        var composition = new TaggedExternalComposition
        {
            ServiceProvider = new ServiceCollection().BuildServiceProvider()
        };

        Should.Throw<CannotResolveException>(() => _ = composition.ExternalConsumer);
    }
}

internal interface IExternalConsumer
{
    IServiceScopeFactory ScopeFactory { get; }
}

internal class ExternalConsumer(
    [Tag("External")] IServiceScopeFactory scopeFactory)
    : IExternalConsumer
{
    public IServiceScopeFactory ScopeFactory { get; } = scopeFactory;
}

internal partial class TaggedExternalComposition : ServiceProviderFactory<TaggedExternalComposition>
{
    private static void Setup() =>
        DI.Setup()
            .Bind<IExternalConsumer>().To<ExternalConsumer>()
            .Root<IExternalConsumer>("ExternalConsumer");
}
