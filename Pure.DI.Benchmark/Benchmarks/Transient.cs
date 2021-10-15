// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable UnusedMember.Local
#pragma warning disable CA1822
namespace Pure.DI.Benchmark.Benchmarks
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Order;
    using Model;
    using Pure.DI;
    
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class Transient: BenchmarkBase
    {
        private static void SetupDI() => DI.Setup()
            .Bind<ICompositionRoot>().To<CompositionRoot>()
            .Bind<IService1>().To<Service1>()
            .Bind<IService2>().To<Service2>()
            .Bind<IService3>().To<Service3>(); 
            
        protected override TActualContainer CreateContainer<TActualContainer, TAbstractContainer>()
        {
            var abstractContainer = new TAbstractContainer();
            abstractContainer.Register(typeof(ICompositionRoot), typeof(CompositionRoot));
            abstractContainer.Register(typeof(IService1), typeof(Service1));
            abstractContainer.Register(typeof(IService2), typeof(Service2));
            abstractContainer.Register(typeof(IService3), typeof(Service3));
            return abstractContainer.TryCreate();
        }
        
        [Benchmark(Description = "Pure.DI", OperationsPerInvoke = 10)]
        public void PureDI()
        {
            TransientDI.Resolve<ICompositionRoot>();
            TransientDI.Resolve<ICompositionRoot>();
            TransientDI.Resolve<ICompositionRoot>();
            TransientDI.Resolve<ICompositionRoot>();
            TransientDI.Resolve<ICompositionRoot>();
            TransientDI.Resolve<ICompositionRoot>();
            TransientDI.Resolve<ICompositionRoot>();
            TransientDI.Resolve<ICompositionRoot>();
            TransientDI.Resolve<ICompositionRoot>();
            TransientDI.Resolve<ICompositionRoot>();
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

        private static ICompositionRoot NewInstance() => 
            new CompositionRoot(new Service1(new Service2(new Service3(), new Service3(), new Service3(), new Service3(), new Service3())), new Service2(new Service3(), new Service3(), new Service3(), new Service3(), new Service3()), new Service2(new Service3(), new Service3(), new Service3(), new Service3(), new Service3()), new Service2(new Service3(), new Service3(), new Service3(), new Service3(), new Service3()), new Service3());
    }
}
#pragma warning restore CA1822