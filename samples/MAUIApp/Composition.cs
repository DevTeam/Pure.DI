// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace MAUIApp;

using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;
using IDispatcher = Clock.ViewModels.IDispatcher;
using ITimer = Clock.Models.ITimer;
using Timer = Clock.Models.Timer;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    private static void Setup() => DI.Setup(nameof(Composition))
        .DependsOn(Base)
        // Specifies not to attempt to resolve types whose fully qualified name
        // begins with Microsoft.Extensions., Microsoft.Maui.
        // since ServiceProvider will be used to retrieve them.
        .Hint(Hint.OnCannotResolveContractTypeNameRegularExpression, "^Microsoft\\.(Extensions|Maui)\\..+$")

        // Roots
        .Root<AppShell>("AppShell")
        .Root<IClockViewModel>("ClockViewModel")
        
        // View Models
        .Bind<IClockViewModel>().As(Singleton).To<ClockViewModel>()

        // Models
        .Bind<ILog<TT>>().To<Log<TT>>()
        .Bind<TimeSpan>().To(_ => TimeSpan.FromSeconds(1))
        .Bind<ITimer>().As(Singleton).To<Timer>()
        .Bind<IClock>().As(PerBlock).To<SystemClock>()
    
        // Infrastructure
        .Bind<IDispatcher>().To<Dispatcher>();
}