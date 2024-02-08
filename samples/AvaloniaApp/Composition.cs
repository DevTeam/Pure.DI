// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace AvaloniaApp;

using Clock.Models;
using Clock.ViewModels;
using Pure.DI;
using Views;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    private static void Setup() => DI.Setup(nameof(Composition))
        // Provides the composition root for main window
        .Root<MainWindow>("MainWindow")
        // Provides the composition root for Clock view model
        .Root<IClockViewModel>("ClockViewModel")
        
        // View Models
        .Bind<IClockViewModel>().As(Singleton).To<ClockViewModel>()

        // Models
        .Bind<ILog<TT>>().To<Log<TT>>()
        .Bind<TimeSpan>().To(_ => TimeSpan.FromSeconds(1))
        .Bind<ITimer>().As(Singleton).To<Clock.Models.Timer>()
        .Bind<IClock>().As(PerBlock).To<SystemClock>()
    
        // Infrastructure
        .Bind<IDispatcher>().To<Dispatcher>();
}