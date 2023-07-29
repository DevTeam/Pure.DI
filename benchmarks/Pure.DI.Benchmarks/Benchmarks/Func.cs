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
        // ThreadSafe = Off
        // FormatCode = On
        // ToString = On
        DI.Setup(nameof(Func))
            .Bind<ICompositionRoot>().To<CompositionRoot>()
            .Bind<IService1>().To<Service1>()
            .Bind<IService2>().To<Service2Func>()
            .Bind<IService3>().To<Service3>()
            .Root<ICompositionRoot>("Root");

    protected override TActualContainer? CreateContainer<TActualContainer, TAbstractContainer>()
        where TActualContainer : class
    {
        var abstractContainer = new TAbstractContainer();
        abstractContainer.Register(typeof(ICompositionRoot), typeof(CompositionRoot));
        abstractContainer.Register(typeof(IService1), typeof(Service1));
        abstractContainer.Register(typeof(IService2), typeof(Service2Func));
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
            new Service1(
                new Service2Func(() => new Service3())),
            new Service2Func(() => new Service3()),
            new Service2Func(() => new Service3()),
            new Service2Func(() => new Service3()),
            new Service3());

    private static readonly Func<IService3> Service3Factory = () => new Service3();
}
#pragma warning restore CA1822