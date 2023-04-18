// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace WpfAppNetCore;

using Clock.Models;
using Clock.ViewModels;
using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    private static void Setup() => DI.Setup(nameof(Composition))
        // View Models
        .Bind<IClockViewModel>().To<ClockViewModel>().Root<IClockViewModel>()

        // Infrastructure
        .Bind<IDispatcher>().To<Dispatcher>()
        
        // Models
        .Bind<ILog<TT>>().To<Log<TT>>()
        .Bind<ITimer>().As(Singleton).To(_ => new Clock.Models.Timer(TimeSpan.FromSeconds(1)))
        .Bind<IClock>().To<SystemClock>();
}