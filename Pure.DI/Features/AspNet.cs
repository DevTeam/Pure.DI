// ReSharper disable PartialTypeWithSinglePart
namespace NS35EBD81B.Features
{
    using System;
    using NS35EBD81B;

    // ReSharper disable once UnusedType.Global
    internal static class AspNet
    {
        private static object ResolveUsingCurrentContext(Type type) { throw new NotImplementedException(); }

        private static void Setup()
        {
            NS35EBD81B.DI.Setup("AspNetFeature")
                .Bind<System.IServiceProvider>()
                    .To(ctx => NS35EBD81B.ServiceProviderInstance.ServiceProvider != null ? NS35EBD81B.ServiceProviderInstance.ServiceProvider: new NS35EBD81B.DefaultServiceProvider(type => ResolveUsingCurrentContext(type)))
                .Bind<Microsoft.Extensions.Logging.ILogger<TT>>()
                    .To(ctx => (Microsoft.Extensions.Logging.ILogger<TT>)ctx.Resolve<System.IServiceProvider>().GetService(typeof(Microsoft.Extensions.Logging.ILogger<TT>)));
        }
    }
}
