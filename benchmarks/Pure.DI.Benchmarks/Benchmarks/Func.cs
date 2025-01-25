// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable UnusedMember.Local

#pragma warning disable CA1822
namespace Pure.DI.Benchmarks.Benchmarks;

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Model;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
[SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known")]
public partial class Func : BenchmarkBase
{
    private static void SetupDI() =>
        DI.Setup()
            .Bind().To<Service1>()
            .Bind().To<Service2Func>()
            .Bind().To<Service3>()
            .Bind().To<Service4>()
            .Root<CompositionRoot>(nameof(TestPureDIByCR), kind: RootKinds.Method | RootKinds.Partial);

    protected override TActualContainer? CreateContainer<TActualContainer, TAbstractContainer>()
        where TActualContainer : class =>
        new TAbstractContainer()
            .Bind(typeof(ICompositionRoot), typeof(CompositionRoot))
            .Bind(typeof(IService1), typeof(Service1))
            .Bind(typeof(IService2), typeof(Service2Func))
            .Bind(typeof(IService3), typeof(Service3))
            .Bind(typeof(IService4), typeof(Service4))
            .TryCreate();

    [Benchmark(Description = "Pure.DI Resolve<T>()")]
    public CompositionRoot TestPureDI() => Resolve<CompositionRoot>();

    [Benchmark(Description = "Pure.DI Resolve(Type)")]
    public object TestPureDINonGeneric() => Resolve(typeof(CompositionRoot));

    [Benchmark(Description = "Pure.DI composition root")]
    public partial CompositionRoot TestPureDIByCR();

    [Benchmark(Description = "Hand Coded", Baseline = true)]
    public CompositionRoot TestHandCoded()
    {
        var func = new Func<IService3>(
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            () => new Service3(new Service4(), new Service4()));
        
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