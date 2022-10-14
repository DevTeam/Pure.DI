// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedType.Global
// ReSharper disable RedundantNameQualifier
namespace Pure.DI.Benchmark.Benchmarks;

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Model;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class ServiceCollection
{
    private IServiceProvider _microsoftServiceProvider;
    private IServiceProvider _crafted;
    private IServiceProvider _pureDIWithServiceCollection;
    private IServiceProvider _pureDI;

    [GlobalSetup]
    public void Setup()
    {
        _microsoftServiceProvider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddTransient<ICompositionRoot, CompositionRoot>()
            .AddSingleton<IService1, Service1>()
            .AddTransient<IService2, Service2>()
            .AddTransient<IService3, Service3>()
            .BuildServiceProvider();
        
        _crafted = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddTransient<ICompositionRoot>(_ => 
                new CompositionRoot(
                    Service1Singleton.Shared,
                    new Service2(
                        new Service3(),
                        new Service3(),
                        new Service3(),
                        new Service3(),
                        new Service3()),
                    new Service2(
                        new Service3(),
                        new Service3(),
                        new Service3(),
                        new Service3(),
                        new Service3()),
                    new Service2(
                        new Service3(),
                        new Service3(),
                        new Service3(),
                        new Service3(),
                        new Service3()),
                    new Service3()))
            .BuildServiceProvider();

        _pureDIWithServiceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddServiceCollectionDI()
            .BuildServiceProvider();
        
        _pureDI = ServiceCollectionDI.Resolve<IServiceProvider>();

        Ioc.Default.ConfigureServices(_microsoftServiceProvider);
    }
    
    [Benchmark(Baseline = true, Description = "MS Service Provider")]
    public ICompositionRoot ServiceProvider() => _microsoftServiceProvider.GetRequiredService<ICompositionRoot>();
    
    [Benchmark(Description = "Crafted")]
    public ICompositionRoot CraftedServiceProvider() => _crafted.GetRequiredService<ICompositionRoot>();
    
    [Benchmark(Description = "Community Toolkit")]
    public ICompositionRoot CommunityToolkit() => Ioc.Default.GetRequiredService<ICompositionRoot>();

    [Benchmark(Description = "Pure.DI")]
    public ICompositionRoot PureDI() => _pureDI.GetRequiredService<ICompositionRoot>();
    
    [Benchmark(Description = "Pure.DI with ServiceCollection")]
    public ICompositionRoot PureDIWithServiceCollection() => _pureDIWithServiceCollection.GetRequiredService<ICompositionRoot>();

    private static void SetupDI() => DI.Setup("ServiceCollectionDI")
        .Bind<ICompositionRoot>().To<CompositionRoot>()
        .Bind<IService1>().As(Pure.DI.Lifetime.Singleton).To<Service1>()
        .Bind<IService2>().To<Service2>()
        .Bind<IService3>().To<Service3>();

    private static class Service1Singleton
    {
        public static readonly IService1 Shared = new Service1(new Service2(new Service3(), new Service3(),new Service3(),new Service3(),new Service3()));
    }
}   