// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable ConvertIfStatementToNullCoalescingAssignment
// ReSharper disable InvertIf
// ReSharper disable UnusedMember.Local
#pragma warning disable CA1822
namespace Pure.DI.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Model;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public partial class Singleton : BenchmarkBase
{
    private static void SetupDI() =>
        // ThreadSafe = Off
        // FormatCode = On
        // ToString = On
        DI.Setup(nameof(Singleton))
            .Bind<ICompositionRoot>().To<CompositionRoot>()
            .Bind<IService1>().As(Lifetime.Singleton).To<Service1>()
            .Bind<IService2>().To<Service2>()
            .Bind<IService3>().To<Service3>()
            .Root<ICompositionRoot>("Root");
    
    protected override TActualContainer? CreateContainer<TActualContainer, TAbstractContainer>()
        where TActualContainer : class
    {
        var abstractContainer = new TAbstractContainer();
        abstractContainer.Register(typeof(ICompositionRoot), typeof(CompositionRoot));
        abstractContainer.Register(typeof(IService1), typeof(Service1), AbstractLifetime.Singleton);
        abstractContainer.Register(typeof(IService2), typeof(Service2));
        abstractContainer.Register(typeof(IService3), typeof(Service3));
        return abstractContainer.TryCreate();
    }

    [Benchmark(Description = "Pure.DI Resolve<T>()")]
    public ICompositionRoot PureDI() => Resolve<ICompositionRoot>();
    
    [Benchmark(Description = "Pure.DI Resolve(Type)")]
    public object PureDINonGeneric() => Resolve(typeof(ICompositionRoot));

    [Benchmark(Description = "Pure.DI composition root")]
    public ICompositionRoot PureDIByCR() => Root;

    [Benchmark(Description = "Hand Coded", Baseline = true)]
    public ICompositionRoot HandCoded() =>
        new CompositionRoot(
            SingletonService1.Shared,
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

    private static class SingletonService1
    {
        public static readonly Service1 Shared = new(new Service2(new Service3(), new Service3(), new Service3(), new Service3(), new Service3()));
    }
}
#pragma warning restore CA1822