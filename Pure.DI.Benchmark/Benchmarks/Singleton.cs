// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable ConvertIfStatementToNullCoalescingAssignment
// ReSharper disable InvertIf
// ReSharper disable UnusedMember.Local
#pragma warning disable CA1822
namespace Pure.DI.Benchmark.Benchmarks
{
    using System.Runtime.CompilerServices;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Order;
    using Model;

    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class Singleton: BenchmarkBase
    {
        private static void SetupDI() => DI.Setup()
            .Bind<ICompositionRoot>().To<CompositionRoot>()
            .Bind<IService1>().As(Lifetime.Singleton).To<Service1>()
            .Bind<IService2>().To<Service2>()
            .Bind<IService3>().To<Service3>();
        
        protected override TActualContainer CreateContainer<TActualContainer, TAbstractContainer>()
        {
            var abstractContainer = new TAbstractContainer();
            abstractContainer.Register(typeof(ICompositionRoot), typeof(CompositionRoot));
            abstractContainer.Register(typeof(IService1), typeof(Service1), AbstractLifetime.Singleton);
            abstractContainer.Register(typeof(IService2), typeof(Service2));
            abstractContainer.Register(typeof(IService3), typeof(Service3));
            return abstractContainer.TryCreate();
        }
        
        [Benchmark(Description = "Pure.DI", OperationsPerInvoke = 10)]
        public void PureDI()
        {
            SingletonDI.Resolve<ICompositionRoot>();
            SingletonDI.Resolve<ICompositionRoot>();
            SingletonDI.Resolve<ICompositionRoot>();
            SingletonDI.Resolve<ICompositionRoot>();
            SingletonDI.Resolve<ICompositionRoot>();
            SingletonDI.Resolve<ICompositionRoot>();
            SingletonDI.Resolve<ICompositionRoot>();
            SingletonDI.Resolve<ICompositionRoot>();
            SingletonDI.Resolve<ICompositionRoot>();
            SingletonDI.Resolve<ICompositionRoot>();
        }
        
        [Benchmark(Description = "Pure.DI composition root", OperationsPerInvoke = 10)]
        public void PureDIByCR()
        {
            SingletonDI.ResolveICompositionRoot();
            SingletonDI.ResolveICompositionRoot();
            SingletonDI.ResolveICompositionRoot();
            SingletonDI.ResolveICompositionRoot();
            SingletonDI.ResolveICompositionRoot();
            SingletonDI.ResolveICompositionRoot();
            SingletonDI.ResolveICompositionRoot();
            SingletonDI.ResolveICompositionRoot();
            SingletonDI.ResolveICompositionRoot();
            SingletonDI.ResolveICompositionRoot();
        }

        [Benchmark(Description = "Hand Coded", OperationsPerInvoke = 10, Baseline = true)]
        public void HandCoded()
        {
            NewInstance();
            NewInstance();
            NewInstance();
            NewInstance();
            NewInstance();
            NewInstance();
            NewInstance();
            NewInstance();
            NewInstance();
            NewInstance();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static ICompositionRoot NewInstance() => 
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
}
#pragma warning restore CA1822