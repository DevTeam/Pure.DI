// ReSharper disable UnusedMember.Local

// ReSharper disable ArrangeTypeMemberModifiers

namespace GrpcService;

using Pure.DI;
using Pure.DI.MS;
using Services;

internal partial class Composition : ServiceProviderFactory<Composition>
{
    static void Setup() => DI.Setup()
        .DependsOn(Base)
        // Specifies not to attempt to resolve types whose fully qualified name
        // begins with Microsoft.Extensions., Microsoft.AspNetCore.
        // since ServiceProvider will be used to retrieve them.
        .Hint(
            Hint.OnCannotResolveContractTypeNameRegularExpression,
            @"^Microsoft\.(Extensions|AspNetCore)\..+$")

        // Provides the composition root for Greeter service
        .Root<GreeterService>();
}