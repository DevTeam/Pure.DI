// ReSharper disable PartialTypeWithSinglePart
namespace Pure.DI.Features
{
    using System;

    // ReSharper disable once UnusedType.Global
    internal static partial class AspNet
    {
        private static object Resolve(Type type) { return default; }

        private static void Setup()
        {
            DI.Setup("AspNetFeature")
                .Bind<System.IServiceProvider>()
                    .To(ctx => Pure.DI.ServiceProviderInstance.ServiceProvider != null ? Pure.DI.ServiceProviderInstance.ServiceProvider: new DefaultServiceProvider(type => Resolve(type)))
                .Bind<Microsoft.Extensions.Logging.ILogger<TT>>()
                    .To(ctx => (Microsoft.Extensions.Logging.ILogger<TT>)ctx.Resolve<System.IServiceProvider>().GetService(typeof(Microsoft.Extensions.Logging.ILogger<TT>)));
        }
    }
}
