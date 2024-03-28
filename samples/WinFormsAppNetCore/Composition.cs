// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable ArrangeTypeMemberModifiers
namespace WinFormsAppNetCore;

using System;
using Clock.Models;
using Clock.ViewModels;
using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    void Setup() => DI.Setup(nameof(Composition))
        // Provides the composition root for main form
        .Root<FormMain>("FormMain")

        // Forms
        .Bind().As(Singleton).To<FormMain>()
        
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