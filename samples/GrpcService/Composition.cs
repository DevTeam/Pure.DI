// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace GrpcService;

using Pure.DI;
using Pure.DI.MS;
using Services;

internal partial class Composition : ServiceProviderFactory<Composition>
{
    static void Setup() => DI.Setup()
        // Provides the composition root for Greeter service
        .Root<GreeterService>();
}