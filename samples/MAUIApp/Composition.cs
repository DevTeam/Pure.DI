// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace MAUIApp;

using Pure.DI;
using Pure.DI.MS;
using IDispatcher = Clock.ViewModels.IDispatcher;
using ITimer = Clock.Models.ITimer;
using Timer = Clock.Models.Timer;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    private static void Setup() => DI.Setup(nameof(Composition))
        .DependsOn(Base)

        // Roots
        .Root<App>("App")
        .Root<IClockViewModel>("ClockViewModel")
        
        // View Models
        .Bind<IClockViewModel>().To<ClockViewModel>()

        // Models
        .Bind<ILog<TT>>().To<Log<TT>>()
        .Bind<TimeSpan>().To(_ => TimeSpan.FromSeconds(1))
        .Bind<ITimer>().As(Lifetime.Singleton).To<Timer>()
        .Bind<IClock>().To<SystemClock>()
    
        // Infrastructure
        .Bind<IDispatcher>().To<Dispatcher>();
}