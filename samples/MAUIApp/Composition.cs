// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace MAUIApp;

using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;
using Timer = Clock.Models.Timer;

partial class Composition: ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root(...)` or `RootBind()` calls.
    private static void Setup() => DI.Setup()
        .DependsOn(Base)
        // Specifies not to attempt to resolve types whose fully qualified name
        // begins with Microsoft.Extensions., Microsoft.Maui.
        // since ServiceProvider will be used to retrieve them.
        .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "Microsoft.Extensions.*")
        .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "Microsoft.Maui.*")

        // Roots
        .Root<AppShell>(nameof(AppShell))
        .Root<IClockViewModel>(nameof(ClockViewModel))
        
        // View Models
        .Bind().As(Singleton).To<ClockViewModel>()

        // Models
        .Bind().To<Log<TT>>()
        .Bind().As(Singleton).To<Timer>()
        .Bind().As(PerBlock).To<SystemClock>()
    
        // Infrastructure
        .Bind().To<Dispatcher>();
}