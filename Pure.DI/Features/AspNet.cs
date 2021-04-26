// ReSharper disable PartialTypeWithSinglePart
namespace Pure.DI.Features
{
    // ReSharper disable once UnusedType.Global
    internal static partial class AspNet
    {
        static AspNet()
        {
            DI.Setup("AspNetFeature")
                .Bind<System.IServiceProvider>().To(ctx => ServiceProviderInstance.ServiceProvider)
                .Bind<Microsoft.Extensions.Logging.ILogger<TT>>().To(ctx => (Microsoft.Extensions.Logging.ILogger<TT>)ctx.Resolve<System.IServiceProvider>()?.GetService(typeof(Microsoft.Extensions.Logging.ILogger<TT>)));
        }
    }
}
