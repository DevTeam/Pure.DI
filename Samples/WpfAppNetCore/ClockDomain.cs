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
            // Infrastructure
            .Bind<IDispatcher>().As(Singleton).To<Dispatcher>()
            .Bind<IMainWindowView>().As(Singleton).To<MainWindow>()

            // View Models
            .Bind<IClockViewModel>().To<ClockViewModel>()

            // Models
            .Bind<ILog<TT>>().As(Singleton).To<Log<TT>>()
            .Bind<ITimer>().As(Singleton).To(_ => new Timer(TimeSpan.FromSeconds(1)))
            .Bind<IClock>().As(Singleton).To<SystemClock>();
    }
}