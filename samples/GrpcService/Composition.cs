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
            // Specifies not to attempt to resolve types whose fully qualified name
            // begins with Microsoft.Extensions., Microsoft.AspNetCore.
            // since ServiceProvider will be used to retrieve them.
            .Hint(
                Hint.OnCannotResolveContractTypeNameRegularExpression,
                @"^Microsoft\.(Extensions|AspNetCore)\..+$")
            
            // Provides the composition root for Greeter service
            .Root<GreeterService>();
}