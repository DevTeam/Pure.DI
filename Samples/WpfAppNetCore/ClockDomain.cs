// ReSharper disable UnusedType.Global
// ReSharper disable PartialTypeWithSinglePart
namespace WpfAppNetCore
{
    using System;
    using Clock.Models;
    using Clock.ViewModels;
    using Pure.DI;
    using Views;
    using static Pure.DI.Lifetime;

    internal static partial class ClockDomain
    {
        static ClockDomain() => DI.Setup()
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