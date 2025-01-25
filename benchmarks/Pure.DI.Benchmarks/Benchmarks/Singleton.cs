// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable ConvertIfStatementToNullCoalescingAssignment
// ReSharper disable InvertIf
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
public partial class Singleton : BenchmarkBase
{
    private static void SetupDI() =>
        DI.Setup()
            .Bind().As(Lifetime.Scoped).To<Service1>()
            .Bind().To<Service2>()
            .Bind().To<Service3>()
            .Bind().As(Lifetime.Scoped).To<Service4>()
            .Root<CompositionRoot>(nameof(TestPureDIByCR), kind: RootKinds.Method | RootKinds.Partial);

    protected override TActualContainer? CreateContainer<TActualContainer, TAbstractContainer>()
        where TActualContainer : class =>
        new TAbstractContainer()
            .Bind(typeof(ICompositionRoot), typeof(CompositionRoot))
            .Bind(typeof(IService1), typeof(Service1), AbstractLifetime.Singleton)
            .Bind(typeof(IService2), typeof(Service2))
            .Bind(typeof(IService3), typeof(Service3))
            .Bind(typeof(IService4), typeof(Service4), AbstractLifetime.Singleton)
            .TryCreate();

    [Benchmark(Description = "Pure.DI Resolve<T>()")]
    public CompositionRoot TestPureDI() => Resolve<CompositionRoot>();

    [Benchmark(Description = "Pure.DI Resolve(Type)")]
    public object TestPureDINonGeneric() => Resolve(typeof(CompositionRoot));

    [Benchmark(Description = "Pure.DI composition root")]
    public partial CompositionRoot TestPureDIByCR();

    [Benchmark(Description = "Hand Coded", Baseline = true)]
    public CompositionRoot TestHandCoded() =>
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