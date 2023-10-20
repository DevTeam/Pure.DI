// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace WinFormsApp;

using System;
using Clock.Models;
using Clock.ViewModels;
using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    private static void Setup() => DI.Setup(nameof(Composition))
        .Root<FormMain>("FormMain")

        // Forms
        .Bind<FormMain>().As(Singleton).To<FormMain>()
        
        // View Models
        .Bind<IClockViewModel>().To<ClockViewModel>()

        // Models
        .Bind<ILog<TT>>().To<Log<TT>>()
        .Bind<TimeSpan>().To(_ => TimeSpan.FromSeconds(1))
        .Bind<ITimer>().As(Singleton).To<Clock.Models.Timer>()
        .Bind<IClock>().To<SystemClock>()
    
        // Infrastructure
        .Bind<IDispatcher>().To<Dispatcher>();
}