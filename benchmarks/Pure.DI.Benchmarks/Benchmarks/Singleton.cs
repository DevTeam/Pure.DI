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
        DI.Setup(nameof(Singleton))
            .Bind<IService1>().As(Lifetime.Singleton).To<Service1>()
            .Bind<IService2>().To<Service2>()
            .Bind<IService3>().To<Service3>()
            .Bind<IService4>().As(Lifetime.Singleton).To<Service4>()
            .Root<CompositionRoot>("PureDIByCR", default, RootKinds.Method | RootKinds.Partial);

    protected override TActualContainer? CreateContainer<TActualContainer, TAbstractContainer>()
        where TActualContainer : class
    {
        var abstractContainer = new TAbstractContainer();
        abstractContainer.Register(typeof(ICompositionRoot), typeof(CompositionRoot));
        abstractContainer.Register(typeof(IService1), typeof(Service1), AbstractLifetime.Singleton);
        abstractContainer.Register(typeof(IService2), typeof(Service2));
        abstractContainer.Register(typeof(IService3), typeof(Service3));
        abstractContainer.Register(typeof(IService4), typeof(Service4), AbstractLifetime.Singleton);
        return abstractContainer.TryCreate();
    }

    [Benchmark(Description = "Pure.DI Resolve<T>()")]
    public CompositionRoot PureDI() => Resolve<CompositionRoot>();

    [Benchmark(Description = "Pure.DI Resolve(Type)")]
    public object PureDINonGeneric() => Resolve(typeof(CompositionRoot));

    [Benchmark(Description = "Pure.DI composition root")]
    public partial CompositionRoot PureDIByCR();

    [Benchmark(Description = "Hand Coded", Baseline = true)]
    public CompositionRoot HandCoded() =>
        new(
            Singletons.Service1,
            new Service2(
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4)),
            new Service2(
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4)),
            new Service2(
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4),
                new Service3(Singletons.Service4, Singletons.Service4)),
            new Service3(Singletons.Service4, Singletons.Service4),
            Singletons.Service4,
            Singletons.Service4);

    private static class Singletons
    {
        public static readonly Service4 Service4 = new();

        public static readonly Service1 Service1 = new(
            new Service2(
                new Service3(Service4, Service4),
                new Service3(Service4, Service4),
                new Service3(Service4, Service4),
                new Service3(Service4, Service4),
                new Service3(Service4, Service4)));
    }
}
#pragma warning restore CA1822