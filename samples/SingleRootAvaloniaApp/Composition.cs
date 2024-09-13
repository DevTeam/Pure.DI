// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable ArrangeTypeMemberModifiers

namespace AvaloniaApp;

using Clock.Models;
using Clock.ViewModels;
using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    void Setup() => DI.Setup()
        // Single composition root for the application
        .Root<Root>(nameof(Root))
        .Bind().As(Singleton).To<Root>()

        // View Models
        .Bind().To<ClockViewModel>()

        // Models
        .Bind().To<Log<TT>>()
        .Bind().To(_ => TimeSpan.FromSeconds(1))
        .Bind().As(Singleton).To<Clock.Models.Timer>()
        .Bind().As(PerBlock).To<SystemClock>()

        // Infrastructure
        .Bind().To<Dispatcher>();
}