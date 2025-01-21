// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable ArrangeTypeMemberModifiers

namespace WpfAppNetCore;

using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    void Setup() => DI.Setup()
        // Provides the composition root for clock view model
        .Root<IClockViewModel>(nameof(ClockViewModel))
        
        // View Models
        .Bind().As(Singleton).To<ClockViewModel>()

        // Models
        .Bind().To<Log<TT>>()
        .Bind().To(_ => TimeSpan.FromSeconds(1))
        .Bind().As(Singleton).To<Clock.Models.Timer>()
        .Bind().As(PerBlock).To<SystemClock>()
    
        // Infrastructure
        .Bind().To<Dispatcher>();
}