// ReSharper disable once CheckNamespace
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageTests.IntegrationTests.MicrosoftDependencyInjectionTests;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;
using MS;

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

    [Fact]
    public void ShouldRegisterValueTypeRootInServiceCollection()
    {
        var composition = new ValueTypeRootComposition();
        var serviceProvider = composition.ServiceCollection.BuildServiceProvider();

        serviceProvider.GetRequiredService<int>().ShouldBe(42);
    }

    [Fact]
    public void ShouldExportPureDiRootLifetimesAsMicrosoftDescriptors()
    {
        var serviceCollection = new LifetimeComposition().ServiceCollection;

        serviceCollection.Single(descriptor => descriptor.ServiceType == typeof(ISingletonService)).Lifetime.ShouldBe(ServiceLifetime.Singleton);
        serviceCollection.Single(descriptor => descriptor.ServiceType == typeof(IScopedService)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        serviceCollection.Single(descriptor => descriptor.ServiceType == typeof(ITransientService)).Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void ShouldResolveServicesAccordingToMicrosoftLifetimes()
    {
        using var serviceProvider = new LifetimeComposition().ServiceCollection.BuildServiceProvider();

        serviceProvider.GetRequiredService<ISingletonService>().ShouldBe(serviceProvider.GetRequiredService<ISingletonService>());
        serviceProvider.GetRequiredService<IScopedService>().ShouldBe(serviceProvider.GetRequiredService<IScopedService>());
        serviceProvider.GetRequiredService<ITransientService>().ShouldNotBe(serviceProvider.GetRequiredService<ITransientService>());
    }

    [Fact]
    public void ShouldDisposePureDiSingletonOnceWhenResolvedMultipleTimesFromMicrosoftProvider()
    {
        DisposableSingletonService.DisposeCount = 0;
        var serviceProvider = new LifetimeComposition().ServiceCollection.BuildServiceProvider();

        serviceProvider.GetRequiredService<ISingletonService>().ShouldBe(serviceProvider.GetRequiredService<ISingletonService>());

        serviceProvider.Dispose();

        DisposableSingletonService.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public void ShouldCreatePureDiScopedRootPerMicrosoftScope()
    {
        using var serviceProvider = new LifetimeComposition().ServiceCollection.BuildServiceProvider();
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        scope1.ServiceProvider.GetRequiredService<IScopedService>()
            .ShouldNotBe(scope2.ServiceProvider.GetRequiredService<IScopedService>());
    }

    [Fact]
    public void ShouldReusePureDiScopedRootWithinMicrosoftScope()
    {
        using var serviceProvider = new LifetimeComposition().ServiceCollection.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IScopedService>()
            .ShouldBe(scope.ServiceProvider.GetRequiredService<IScopedService>());
    }

    [Fact]
    public void ShouldDisposePureDiScopedRootOnceWhenMicrosoftScopeIsDisposed()
    {
        DisposableScopedService.DisposeCount = 0;
        var serviceProvider = new LifetimeComposition().ServiceCollection.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IScopedService>()
            .ShouldBe(scope.ServiceProvider.GetRequiredService<IScopedService>());

        scope.Dispose();
        serviceProvider.Dispose();

        DisposableScopedService.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public void ShouldResolveScopedDependencyOfTransientRootFromTheCurrentScope()
    {
        using var serviceProvider = new MixedLifetimeComposition().ServiceCollection.BuildServiceProvider();
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var transient1 = scope1.ServiceProvider.GetRequiredService<ITransientConsumer>();
        var transient2 = scope2.ServiceProvider.GetRequiredService<ITransientConsumer>();
        var scoped1 = scope1.ServiceProvider.GetRequiredService<IScopedService>();
        var scoped2 = scope2.ServiceProvider.GetRequiredService<IScopedService>();

        transient1.Dependency.ShouldBe(scoped1);
        transient2.Dependency.ShouldBe(scoped2);
        transient1.Dependency.ShouldNotBe(transient2.Dependency);
    }

    [Fact]
    public void ShouldResolveScopedDependencyOfTransientRootInsideTheSameScope()
    {
        using var serviceProvider = new MixedLifetimeComposition().ServiceCollection.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITransientConsumer>().Dependency
            .ShouldBe(scope.ServiceProvider.GetRequiredService<ITransientConsumer>().Dependency);
    }

    [Fact]
    public void ShouldNotUseScopedDependencyOfParentCompositionForTransientRoot()
    {
        var composition = new MixedLifetimeComposition();
        using var serviceProvider = composition.ServiceCollection.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var scopedInScope = scope.ServiceProvider.GetRequiredService<IScopedService>();

        scope.ServiceProvider.GetRequiredService<ITransientConsumer>().Dependency
            .ShouldNotBe(composition.Resolve<IScopedService>());
        scope.ServiceProvider.GetRequiredService<ITransientConsumer>().Dependency
            .ShouldBe(scopedInScope);
    }

    [Fact]
    public void ShouldResolveScopedDependencyOfPerResolveRootFromTheCurrentScope()
    {
        using var serviceProvider = new MixedLifetimeComposition().ServiceCollection.BuildServiceProvider();
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        scope1.ServiceProvider.GetRequiredService<IPerResolveConsumer>().Dependency
            .ShouldBe(scope1.ServiceProvider.GetRequiredService<IScopedService>());
        scope2.ServiceProvider.GetRequiredService<IPerResolveConsumer>().Dependency
            .ShouldBe(scope2.ServiceProvider.GetRequiredService<IScopedService>());
    }

    [Fact]
    public void ShouldResolveScopedDependencyOfPerBlockRootFromTheCurrentScope()
    {
        using var serviceProvider = new MixedLifetimeComposition().ServiceCollection.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IPerBlockConsumer>().Dependency
            .ShouldBe(scope.ServiceProvider.GetRequiredService<IScopedService>());
    }

    [Fact]
    public void ShouldReusePureDiTransientRootWithScopedDependencyWithinMicrosoftScope()
    {
        DisposableScopedService.DisposeCount = 0;
        var serviceProvider = new MixedLifetimeComposition().ServiceCollection.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();

        var dependency = scope.ServiceProvider.GetRequiredService<ITransientConsumer>().Dependency;
        dependency.ShouldBe(scope.ServiceProvider.GetRequiredService<IScopedService>());

        scope.Dispose();
        serviceProvider.Dispose();

        // The scoped instance the transient root captured is disposed with its own scope.
        DisposableScopedService.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public void ShouldKeepSingletonRootSharedWhenCompositionHasScopedRoots()
    {
        // A singleton root cannot depend on a scoped service (DIE041), so it depends on a
        // transient one. What is checked here is that the presence of scoped roots does not make
        // the singleton start resolving per scope.
        var composition = new MixedLifetimeComposition();
        using var serviceProvider = composition.ServiceCollection.BuildServiceProvider();
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var singleton1 = scope1.ServiceProvider.GetRequiredService<ISingletonRoot>();
        var singleton2 = scope2.ServiceProvider.GetRequiredService<ISingletonRoot>();

        singleton1.ShouldBe(singleton2);
        serviceProvider.GetRequiredService<ISingletonRoot>().ShouldBe(singleton1);
    }

    [Fact]
    public void ShouldResolveRootsOfCompositionWithoutScopedRoots()
    {
        // There is no scoped root here, so no scope holder is registered and resolving must
        // neither fail nor depend on a scope.
        using var serviceProvider = new TransientOnlyComposition().ServiceCollection.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITransientService>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<ITransientService>()
            .ShouldNotBe(scope.ServiceProvider.GetRequiredService<ITransientService>());
        serviceProvider.GetRequiredService<ITransientService>().ShouldNotBeNull();
    }

    [Fact]
    public void ShouldThrowWhenScopedRootIsResolvedFromRootProviderWithScopeValidation()
    {
        var composition = new MixedLifetimeComposition();
        using var serviceProvider = composition.ServiceCollection
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IScopedService>());
    }

    [Fact]
    public void ShouldThrowWhenTransientRootIsResolvedFromRootProviderWithScopeValidation()
    {
        // A root of a shorter lifetime is bound to a Microsoft scope now, so resolving it from the
        // root provider is reported by the scope validation of Microsoft DI, exactly as for any
        // other scoped service. Inside a scope it resolves fine.
        var composition = new MixedLifetimeComposition();
        using var serviceProvider = composition.ServiceCollection
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<ITransientConsumer>());

        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITransientConsumer>().ShouldNotBeNull();
    }
}

interface IExternalConsumer
{
    IServiceScopeFactory ScopeFactory { get; }
}

class ExternalConsumer(
    [Tag("External")] IServiceScopeFactory scopeFactory)
    : IExternalConsumer
{
    public IServiceScopeFactory ScopeFactory { get; } = scopeFactory;
}

partial class TaggedExternalComposition : ServiceProviderFactory<TaggedExternalComposition>
{
    private static void Setup() =>
        DI.Setup()
            .Bind<IExternalConsumer>().To<ExternalConsumer>()
            .Root<IExternalConsumer>("ExternalConsumer");
}

partial class ValueTypeRootComposition : ServiceProviderFactory<ValueTypeRootComposition>
{
    public IServiceCollection ServiceCollection =>
        CreateServiceCollection(this);

    private static void Setup() =>
        DI.Setup()
            .Bind<int>().To(_ => 42)
            .Root<int>("Number");
}

interface ISingletonService;

interface IScopedService;

interface ITransientService;

class DisposableSingletonService : ISingletonService, IDisposable
{
    public static int DisposeCount;

    public void Dispose() => DisposeCount++;
}

class DisposableScopedService : IScopedService, IDisposable
{
    public static int DisposeCount;

    public void Dispose() => DisposeCount++;
}

class TransientService : ITransientService;

partial class LifetimeComposition : ServiceProviderFactory<LifetimeComposition>
{
    public IServiceCollection ServiceCollection =>
        CreateServiceCollection(this);

    private static void Setup() =>
        DI.Setup()
            .Bind<ISingletonService>().As(Lifetime.Singleton).To<DisposableSingletonService>()
            .Bind<IScopedService>().As(Lifetime.Scoped).To<DisposableScopedService>()
            .Bind<ITransientService>().As(Lifetime.Transient).To<TransientService>()
            .Root<ISingletonService>("Singleton")
            .Root<IScopedService>("Scoped")
            .Root<ITransientService>("Transient");
}

interface ITransientConsumer
{
    IScopedService Dependency { get; }
}

class TransientConsumer(IScopedService dependency) : ITransientConsumer
{
    public IScopedService Dependency { get; } = dependency;
}

interface IPerResolveConsumer
{
    IScopedService Dependency { get; }
}

class PerResolveConsumer(IScopedService dependency) : IPerResolveConsumer
{
    public IScopedService Dependency { get; } = dependency;
}

interface IPerBlockConsumer
{
    IScopedService Dependency { get; }
}

class PerBlockConsumer(IScopedService dependency) : IPerBlockConsumer
{
    public IScopedService Dependency { get; } = dependency;
}

interface ISingletonRoot
{
    ITransientService Dependency { get; }
}

class SingletonRoot(ITransientService dependency) : ISingletonRoot
{
    public ITransientService Dependency { get; } = dependency;
}

/// <summary>
/// Covers every Pure.DI lifetime that maps to a Microsoft dependency injection lifetime and
/// depends on the scoped service, so that each one can be checked against the current scope.
/// </summary>
partial class MixedLifetimeComposition : ServiceProviderFactory<MixedLifetimeComposition>
{
    public IServiceCollection ServiceCollection =>
        CreateServiceCollection(this);

    private static void Setup() =>
        DI.Setup()
            .Bind<IScopedService>().As(Lifetime.Scoped).To<DisposableScopedService>()
            .Bind<ITransientService>().As(Lifetime.Transient).To<TransientService>()
            .Bind<ITransientConsumer>().As(Lifetime.Transient).To<TransientConsumer>()
            .Bind<IPerResolveConsumer>().As(Lifetime.PerResolve).To<PerResolveConsumer>()
            .Bind<IPerBlockConsumer>().As(Lifetime.PerBlock).To<PerBlockConsumer>()
            .Bind<ISingletonRoot>().As(Lifetime.Singleton).To<SingletonRoot>()
            .Root<IScopedService>("Scoped")
            .Root<ITransientConsumer>("TransientConsumer")
            .Root<IPerResolveConsumer>("PerResolveConsumer")
            .Root<IPerBlockConsumer>("PerBlockConsumer")
            .Root<ISingletonRoot>("SingletonRoot");
}

/// <summary>
/// A composition without any scoped root: no scope holder is registered for it.
/// </summary>
partial class TransientOnlyComposition : ServiceProviderFactory<TransientOnlyComposition>
{
    public IServiceCollection ServiceCollection =>
        CreateServiceCollection(this);

    private static void Setup() =>
        DI.Setup()
            .Bind<ITransientService>().As(Lifetime.Transient).To<TransientService>()
            .Root<ITransientService>("Transient");
}
