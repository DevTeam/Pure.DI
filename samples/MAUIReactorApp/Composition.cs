// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace MAUIReactorApp;

using Clock.ViewModels;
using Components;
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;
using Timer = Clock.Models.Timer;

partial class Composition: ServiceProviderFactory<Composition>
{
    private static void Setup() => DI.Setup()
        .DependsOn(Base)
        // Specifies not to attempt to resolve types whose fully qualified name
        // begins with Microsoft.Extensions., Microsoft.Maui.
        // since ServiceProvider will be used to retrieve them.
        .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "Microsoft.Extensions.*")
        .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "Microsoft.Maui.*")

        // Roots
        .Root<Composition>()

        // Builders
        .Builder<ClockPage>()

        // View Models
        .Bind().As(Singleton).To<ClockViewModel>()

        // Models
        .Bind().To<Log<TT>>()
        .Bind().As(Singleton).To<Timer>()
        .Bind().As(PerBlock).To<SystemClock>()
    
        // Infrastructure
        .Bind().To<Dispatcher>();
}