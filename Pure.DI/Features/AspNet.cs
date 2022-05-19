// ReSharper disable PartialTypeWithSinglePart
namespace NS35EBD81B.Features
{
    using System;
    using NS35EBD81B;

    // ReSharper disable once UnusedType.Global
    internal static class AspNet
    {
        private static object Resolve(Type type) { throw new NotImplementedException(); }

        private static void Setup()
        {
            NS35EBD81B.DI.Setup("AspNetFeature")
                .Bind<System.IServiceProvider>(typeof(NS35EBD81B.DefaultServiceProvider))
                    .As(Lifetime.Singleton)
                    .To(ctx => new NS35EBD81B.DefaultServiceProvider(type => Resolve(type)))
                .Bind<System.IServiceProvider>()
                    .To(ctx => NS35EBD81B.ServiceProviderInstance.ServiceProvider != null ? NS35EBD81B.ServiceProviderInstance.ServiceProvider: ctx.Resolve<System.IServiceProvider>(typeof(NS35EBD81B.DefaultServiceProvider)))
                .Bind<Microsoft.Extensions.Logging.ILogger<TT>>()
                    .To(ctx => (Microsoft.Extensions.Logging.ILogger<TT>)ctx.Resolve<System.IServiceProvider>().GetService(typeof(Microsoft.Extensions.Logging.ILogger<TT>)));
        }
    }
}
