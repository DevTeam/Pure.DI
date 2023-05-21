// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace WpfAppNetCore;

using Pure.DI;

internal partial class Composition
{
    private static void Setup() => DI.Setup(nameof(Composition))
        // View Models
        .Bind<IClockViewModel>().To<ClockViewModel>().Root<IClockViewModel>("ClockViewModel")

        // Models
        .Bind<ILog<TT>>().To<Log<TT>>()
        .Bind<TimeSpan>().To(_ => TimeSpan.FromSeconds(1))
        .Bind<ITimer>().As(Lifetime.Singleton).To<Clock.Models.Timer>()
        .Bind<IClock>().To<SystemClock>()
    
        // Infrastructure
        .Bind<IDispatcher>().To<Dispatcher>();
}