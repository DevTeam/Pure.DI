// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable ArrangeTypeMemberModifiers

namespace WinFormsAppNetCore;

using Clock.Models;
using Clock.ViewModels;
using Pure.DI;
using static Pure.DI.Lifetime;

partial class Composition
{
    void Setup() => DI.Setup()
        // Provides the composition root for main form
        .Root<Owned<FormMain>>(nameof(Root))

        // Forms
        .Bind().As(Singleton).To<FormMain>()

        // View Models
        .Bind().To<ClockViewModel>()

        // Models
        .Bind().To<Log<TT>>()
        .Bind().To(_ => TimeSpan.FromSeconds(1))
        .Bind().To<Timer>()
        .Bind().As(PerBlock).To<SystemClock>()

        // Infrastructure
        .Bind().To<Dispatcher>();
}