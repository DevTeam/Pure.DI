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

        [Benchmark(Description = "new", OperationsPerInvoke = 10)]
        public void New()
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

        private readonly object LockObject = new();
        private volatile Service1 _service1;

        [MethodImpl((MethodImplOptions)0x100)]
        private ICompositionRoot NewInstance()
        {
            if (_service1 == null)
            {
                lock (LockObject)
                {
                    if (_service1 == null)
                    {
                        _service1 = new Service1(new Service2(new Service3(), new Service3(), new Service3(), new Service3(), new Service3()));
                    }
                }
            }

            return new CompositionRoot(_service1, new Service2(new Service3(), new Service3(), new Service3(), new Service3(), new Service3()), new Service2(new Service3(), new Service3(), new Service3(), new Service3(), new Service3()), new Service2(new Service3(), new Service3(), new Service3(), new Service3(), new Service3()), new Service3());
        }
    }
}
#pragma warning restore CA1822