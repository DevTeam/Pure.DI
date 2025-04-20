// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable ArrangeTypeMemberModifiers

namespace WpfAppNetCore;

using Pure.DI;
using static Pure.DI.Lifetime;
using Timer = Clock.Models.Timer;

partial class Composition
{
    void Setup() => DI.Setup()
        // Provides the composition root for the clock view model
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