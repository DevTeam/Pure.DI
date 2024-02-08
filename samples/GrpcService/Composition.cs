// ReSharper disable UnusedMember.Local

namespace GrpcService;

using Pure.DI;
using Pure.DI.MS;
using Services;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .DependsOn(Base)
            // Provides the composition root for Greeter service
            .Root<GreeterService>();
}