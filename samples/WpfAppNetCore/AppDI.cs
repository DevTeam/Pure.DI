// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace WpfAppNetCore;

using Clock.Models;
using Clock.ViewModels;
using Pure.DI;
using static Pure.DI.Lifetime;

internal static class AppDI
{
    private static void Setup() => DI.Setup("Composition")
        // View Models
        .Bind<IClockViewModel>().To<ClockViewModel>()

        // Singletons:
        .DefaultLifetime(Singleton)

        // Infrastructure
        .Bind<IDispatcher>().To<Dispatcher>()
        
        // Models
        .Bind<ILog<TT>>().To<Log<TT>>()
        .Bind<ITimer>().To(_ => new Clock.Models.Timer(TimeSpan.FromSeconds(1)))
        .Bind<IClock>().To<SystemClock>();
        
    private static void SetupDesignTime() => DI.Setup("DesignTimeComposition")
        // Design-time View Models
        .Bind<IClockViewModel>().To<ClockViewModelDesignTime>();    
}