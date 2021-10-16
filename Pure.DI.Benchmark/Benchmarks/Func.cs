// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable UnusedMember.Local
#pragma warning disable CA1822
namespace Pure.DI.Benchmark.Benchmarks
{
    using System;
    using System.Runtime.CompilerServices;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Order;
    using Model;

    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class Func: BenchmarkBase
    {
        private static void SetupDI() => DI.Setup()
            .Bind<ICompositionRoot>().To<CompositionRoot>()
            .Bind<IService1>().To<Service1>()
            .Bind<IService2>().To<Service2Func>()
            .Bind<IService3>().To<Service3>();
        
        protected override TActualContainer CreateContainer<TActualContainer, TAbstractContainer>()
        {
            var abstractContainer = new TAbstractContainer();
            abstractContainer.Register(typeof(ICompositionRoot), typeof(CompositionRoot));
            abstractContainer.Register(typeof(IService1), typeof(Service1));
            abstractContainer.Register(typeof(IService2), typeof(Service2Func));
            abstractContainer.Register(typeof(IService3), typeof(Service3));
            return abstractContainer.TryCreate();
        }
        
        [Benchmark(Description = "Pure.DI", OperationsPerInvoke = 10)]
        public void PureDI()
        {
            FuncDI.Resolve<ICompositionRoot>();
            FuncDI.Resolve<ICompositionRoot>();
            FuncDI.Resolve<ICompositionRoot>();
            FuncDI.Resolve<ICompositionRoot>();
            FuncDI.Resolve<ICompositionRoot>();
            FuncDI.Resolve<ICompositionRoot>();
            FuncDI.Resolve<ICompositionRoot>();
            FuncDI.Resolve<ICompositionRoot>();
            FuncDI.Resolve<ICompositionRoot>();
            FuncDI.Resolve<ICompositionRoot>();
        }
        
        [Benchmark(Description = "Pure.DI composition root", OperationsPerInvoke = 10)]
        public void PureDIByCR()
        {
            FuncDI.ResolveICompositionRoot();
            FuncDI.ResolveICompositionRoot();
            FuncDI.ResolveICompositionRoot();
            FuncDI.ResolveICompositionRoot();
            FuncDI.ResolveICompositionRoot();
            FuncDI.ResolveICompositionRoot();
            FuncDI.ResolveICompositionRoot();
            FuncDI.ResolveICompositionRoot();
            FuncDI.ResolveICompositionRoot();
            FuncDI.ResolveICompositionRoot();
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

        private static readonly Func<IService3> Service3Factory = () => new Service3();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static ICompositionRoot NewInstance() => 
            new CompositionRoot(new Service1(new Service2Func(Service3Factory)), new Service2Func(Service3Factory), new Service2Func(Service3Factory), new Service2Func(Service3Factory), new Service3());
    }
}
#pragma warning restore CA1822