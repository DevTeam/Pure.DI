// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace WpfAppNetCore;

using System.Diagnostics;
using Pure.DI;

internal partial class Composition
{
    [Conditional("DI")]
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