// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable ArrangeTypeMemberModifiers

namespace AvaloniaApp;

using Clock.Models;
using Clock.ViewModels;
using Pure.DI;
using Views;
using static Pure.DI.Lifetime;

partial class Composition
{
    void Setup() => DI.Setup()
        // Provides the composition root for main window
        .Root<MainWindow>(nameof(MainWindow))
        // Provides the composition root for Clock view model
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