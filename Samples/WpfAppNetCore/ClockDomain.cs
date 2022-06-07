// ReSharper disable UnusedType.Global
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeNamespaceBody
namespace WpfAppNetCore
{
    using System.Diagnostics;
    using Clock.Models;
    using Clock.ViewModels;
    using Pure.DI;
    using Views;
    using static Pure.DI.Lifetime;

    internal static partial class ClockDomain
    {
        [Conditional("DEBUG")] // To exclude this method from a compilation
        private static void Setup() => DI.Setup()
            // Set default lifetime
            .Default(Singleton)

            // Infrastructure
            .Bind<IDispatcher>().To<Dispatcher>()
            .Bind<IMainWindowView>().To<MainWindow>()

            // View Models
            .Bind<IClockViewModel>().As(Transient).To<ClockViewModel>()

            // Models
            .Bind<ILog<TT>>().To<Log<TT>>()
            .Bind<ITimer>().To(_ => new Timer(TimeSpan.FromSeconds(1)))
            .Bind<IClock>().To<SystemClock>();
    }
}