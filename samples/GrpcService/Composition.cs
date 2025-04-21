// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace GrpcService;

using Pure.DI;
using Pure.DI.MS;
using Services;

partial class Composition : ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root(...)` or `RootBind()` calls.
    static void Setup() => DI.Setup()
        // Use the DI setup from the base class
        .DependsOn(Base)
        // Provides the composition root for Greeter service
        .Root<GreeterService>();
}