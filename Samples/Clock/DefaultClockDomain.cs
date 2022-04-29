// ReSharper disable UnusedType.Global
// ReSharper disable PartialTypeWithSinglePart
namespace Clock;

using Models;
using ViewModels;

public static partial class DefaultClockDomain
{
    static DefaultClockDomain() =>
        DI.Setup()
            // View Models
            .Bind<IClockViewModel>().To<ClockViewModel>()

            // Models
            .Bind<ILog<TT>>().As(Lifetime.Singleton).To<Log<TT>>()
            .Bind<ITimer>().As(Lifetime.Singleton).To(_ => new Timer(TimeSpan.FromSeconds(1)))
            .Bind<IClock>().As(Lifetime.Singleton).To<SystemClock>();
}