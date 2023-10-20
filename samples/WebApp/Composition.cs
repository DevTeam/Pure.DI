// ReSharper disable UnusedMember.Local

namespace WebApp;

using Pure.DI;
using Pure.DI.MS;
using Controllers;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .DependsOn(Base)
            .Root<HomeController>();
}