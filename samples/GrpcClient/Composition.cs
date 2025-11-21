using Pure.DI;
using static Pure.DI.Lifetime;
// ReSharper disable UnusedMember.Local

namespace GrpcClient;

partial class Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Root<Program>(nameof(Root))

        .Bind().To<ConsoleAdapter>()
        .Bind<ChannelBase>().As(Singleton).To(_ => GrpcChannel.ForAddress("http://localhost:5000"));
}