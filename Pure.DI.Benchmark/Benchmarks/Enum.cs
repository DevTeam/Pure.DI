// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable UnusedMember.Local
#pragma warning disable CA1822
namespace Pure.DI.Benchmark.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Order;
    using Model;

    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class Enum : BenchmarkBase
    {
        private static void SetupDI() => DI.Setup()
            .Bind<ICompositionRoot>().To<CompositionRoot>()
            .Bind<IService1>().To<Service1>()
            .Bind<IService2>().To<Service2Enum>()
            .Bind<IService3>().To<Service3>()
            .Bind<IService3>().Tags(2).To<Service3v2>()
            .Bind<IService3>().Tags(3).To<Service3v3>()
            .Bind<IService3>().Tags(4).To<Service3v4>();
        
        protected override TActualContainer CreateContainer<TActualContainer, TAbstractContainer>()
        {
            var abstractContainer = new TAbstractContainer();
            abstractContainer.Register(typeof(ICompositionRoot), typeof(CompositionRoot));
            abstractContainer.Register(typeof(IService1), typeof(Service1));
            abstractContainer.Register(typeof(IService2), typeof(Service2Enum));
            abstractContainer.Register(typeof(IService3), typeof(Service3));
            abstractContainer.Register(typeof(IService3), typeof(Service3v2), AbstractLifetime.Transient, "2");
            abstractContainer.Register(typeof(IService3), typeof(Service3v3), AbstractLifetime.Transient, "3");
            abstractContainer.Register(typeof(IService3), typeof(Service3v4), AbstractLifetime.Transient, "4");
            return abstractContainer.TryCreate();
        }
        
        [Benchmark(Description = "Pure.DI", OperationsPerInvoke = 10)]
        public void PureDI()
        {
            EnumDI.Resolve<ICompositionRoot>();
            EnumDI.Resolve<ICompositionRoot>();
            EnumDI.Resolve<ICompositionRoot>();
            EnumDI.Resolve<ICompositionRoot>();
            EnumDI.Resolve<ICompositionRoot>();
            EnumDI.Resolve<ICompositionRoot>();
            EnumDI.Resolve<ICompositionRoot>();
            EnumDI.Resolve<ICompositionRoot>();
            EnumDI.Resolve<ICompositionRoot>();
            EnumDI.Resolve<ICompositionRoot>();
        }
        
        [Benchmark(Description = "Pure.DI composition root", OperationsPerInvoke = 10)]
        public void PureDIByCR()
        {
            EnumDI.ResolveICompositionRoot();
            EnumDI.ResolveICompositionRoot();
            EnumDI.ResolveICompositionRoot();
            EnumDI.ResolveICompositionRoot();
            EnumDI.ResolveICompositionRoot();
            EnumDI.ResolveICompositionRoot();
            EnumDI.ResolveICompositionRoot();
            EnumDI.ResolveICompositionRoot();
            EnumDI.ResolveICompositionRoot();
            EnumDI.ResolveICompositionRoot();
        }

        [Benchmark(Description = "Hand Coded", OperationsPerInvoke = 10)]
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

        private static readonly Func<IService3> Service3Factory = () => new Service3();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static ICompositionRoot NewInstance() =>
            new CompositionRoot(
                new Service1(
                    new Service2Enum(EnumerableOfIService3())), 
                new Service2Enum(EnumerableOfIService3()),
                new Service2Enum(EnumerableOfIService3()),
                new Service2Enum(EnumerableOfIService3()),
                new Service3());
        
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static IEnumerable<IService3> EnumerableOfIService3()
        {
            yield return new Service3();
            yield return new Service3v2();
            yield return new Service3v3();
            yield return new Service3v4();
        }
    }
}
#pragma warning disable CA1822