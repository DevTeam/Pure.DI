// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedType.Global
// ReSharper disable RedundantNameQualifier
namespace Pure.DI.Benchmarks.Benchmarks;

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using Model;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ServiceProvider
{
#pragma warning disable CS8618
    private IServiceProvider _microsoft;
    private IServiceProvider _lambdaMicrosoft;
    // private IServiceProvider _pureDIServiceCollection;
    // private IServiceProvider _pureDI;
#pragma warning restore CS8618

    [GlobalSetup]
    public void Setup()
    {
        _microsoft = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddTransient<ICompositionRoot, CompositionRoot>()
            .AddSingleton<IService1, Service1>()
            .AddTransient<IService2, Service2>()
            .AddTransient<IService3, Service3>()
            .BuildServiceProvider();
        
        _lambdaMicrosoft = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddTransient(CreateCompositionRoot)
            .BuildServiceProvider();

        DI.Setup("ServiceCollectionDI")
            .Bind<ICompositionRoot>().To<CompositionRoot>()
            .Bind<IService1>().As(Lifetime.Singleton).To<Service1>()
            .Bind<IService2>().To<Service2>()
            .Bind<IService3>().To<Service3>();

        /*_pureDIServiceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddServiceCollectionDI()
            .BuildServiceProvider();
        
        _pureDI = ServiceCollectionDI.Resolve<IServiceProvider>();*/
    }

    [Benchmark(Baseline = true, Description = "Microsoft")]
    public object? Microsoft() => _microsoft.GetService(typeof(ICompositionRoot));
    
    [Benchmark(Description = "Microsoft based on Lambda")]
    public object? MicrosoftLambda() => _lambdaMicrosoft.GetService(typeof(ICompositionRoot));
    
    /*[Benchmark(Description = "Pure.DI")]
    public object? PureDI() => _pureDI.GetService(typeof(ICompositionRoot));
    
    [Benchmark(Description = "Pure.DI based on ServiceCollection")]
    public object? PureDIServiceCollection() => _pureDIServiceCollection.GetService(typeof(ICompositionRoot));*/
    
    private static ICompositionRoot CreateCompositionRoot(IServiceProvider _) => 
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
            new Service3());

    private static class Service1Singleton
    {
        public static readonly IService1 Shared =
            new Service1(
                new Service2(
                    new Service3(), 
                    new Service3(), 
                    new Service3(), 
                    new Service3(), 
                    new Service3()));
    }
}   