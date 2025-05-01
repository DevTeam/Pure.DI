// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace MAUIReactorApp;

using Clock.ViewModels;
using Components;
using Pure.DI;
using Pure.DI.MS;
using Clock.Models;
using static Pure.DI.Lifetime;

partial class Composition: ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root(...)` or `RootBind()` calls.
    private static void Setup() => DI.Setup()
        .DependsOn(Base)
        .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "Microsoft.Extensions.*")
        .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "Microsoft.AspNetCore.*")
        .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "Microsoft.Maui.*")
        .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "System.Net.Http*")

        // Roots
        .Root<Composition>()
        .Root<IService>()

        // Builders
        .Builder<ClockPage>()

        .Bind().As(Singleton).To<MyService>()
        .Bind().As(Singleton).To<ApiHelper>()

        // View Models
        .Bind().As(Singleton).To<ClockViewModel>()

        // Models
        .Bind().To<Log<TT>>()
        .Bind().As(Singleton).To<Timer>()
        .Bind().As(PerBlock).To<SystemClock>()
    
        // Infrastructure
        .Bind().To<Dispatcher>();
}