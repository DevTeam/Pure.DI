// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable UnusedMember.Local

#pragma warning disable CA1822
namespace Pure.DI.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Model;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public partial class Func : BenchmarkBase
{
    private static void SetupDI() =>
        DI.Setup(nameof(Func))
            .Bind<IService1>().To<Service1>()
            .Bind<IService2>().To<Service2Func>()
            .Bind<IService3>().To<Service3>()
            .Bind<IService4>().To<Service4>()
            .Root<CompositionRoot>("PureDIByCR", default, RootKinds.Method | RootKinds.Partial);

    protected override TActualContainer? CreateContainer<TActualContainer, TAbstractContainer>()
        where TActualContainer : class
    {
        var abstractContainer = new TAbstractContainer();
        abstractContainer.Register(typeof(ICompositionRoot), typeof(CompositionRoot));
        abstractContainer.Register(typeof(IService1), typeof(Service1));
        abstractContainer.Register(typeof(IService2), typeof(Service2Func));
        abstractContainer.Register(typeof(IService3), typeof(Service3));
        abstractContainer.Register(typeof(IService4), typeof(Service4));
        return abstractContainer.TryCreate();
    }

    [Benchmark(Description = "Pure.DI Resolve<T>()")]
    public CompositionRoot PureDI() => Resolve<CompositionRoot>();

    [Benchmark(Description = "Pure.DI Resolve(Type)")]
    public object PureDINonGeneric() => Resolve(typeof(CompositionRoot));

    [Benchmark(Description = "Pure.DI composition root")]
    public partial CompositionRoot PureDIByCR();

    [Benchmark(Description = "Hand Coded", Baseline = true)]
    public CompositionRoot HandCoded()
    {
        var func = new Func<IService3>(() => new Service3(new Service4(), new Service4()));
        return new CompositionRoot(
            new Service1(new Service2Func(func)),
            new Service2Func(func),
            new Service2Func(func),
            new Service2Func(func),
            new Service3(new Service4(), new Service4()),
            new Service4(),
            new Service4());
    }
}
#pragma warning restore CA1822