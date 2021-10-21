namespace Pure.DI.Benchmark
{
    using System;
    using Autofac;
    using BenchmarkDotNet.Attributes;
    using Castle.Windsor;
    using Containers;
    using DryIoc;
    using IoC;
    using LightInject;
    using Microsoft.Extensions.DependencyInjection;
    using Ninject;
    using Unity;
    using Container = DryIoc.Container;
    using ICompositionRoot = Model.ICompositionRoot;

    public abstract class BenchmarkBase
    {
        private IoC.Container _iocContainer;
        private Func<ICompositionRoot> _iocRootResolver;
        private global::Autofac.IContainer _autofacContainer;
        private WindsorContainer _windsorContainerContainer;
        private Container _dryIocContainer;
        private ServiceContainer _lightInjectContainer;
        private StandardKernel _ninjectContainer;
        private UnityContainer _unityContainer;
        private ServiceProvider _microsoftContainer;
        private global::SimpleInjector.Container _simpleInjectorContainer;

        protected abstract TContainer CreateContainer<TContainer, TAbstractContainer>() where TAbstractContainer : IAbstractContainer<TContainer>, new();

        [GlobalSetup]
        public void Setup()
        {
            _iocContainer = CreateContainer<IoC.Container, IoCContainer>();
            _iocRootResolver = CreateContainer<Func<ICompositionRoot>, IoCContainerByCompositionRoot<ICompositionRoot>>();
            _autofacContainer = CreateContainer<global::Autofac.IContainer, Autofac>();
            _windsorContainerContainer = CreateContainer<WindsorContainer, CastleWindsor>();
            _dryIocContainer = CreateContainer<Container, DryIoc>();
            _lightInjectContainer = CreateContainer<ServiceContainer, LightInject>();
            _ninjectContainer = CreateContainer<StandardKernel, Ninject>();
            _unityContainer = CreateContainer<UnityContainer, Unity>();
            _microsoftContainer = CreateContainer<ServiceProvider, MicrosoftDependencyInjection>();
            _simpleInjectorContainer = CreateContainer<global::SimpleInjector.Container, SimpleInjector>();
        }

        [Benchmark(Description = "IoC.Container", OperationsPerInvoke = 10)]
        public void IoCContainer()
        {
            _iocContainer.Resolve<ICompositionRoot>();
            _iocContainer.Resolve<ICompositionRoot>();
            _iocContainer.Resolve<ICompositionRoot>();
            _iocContainer.Resolve<ICompositionRoot>();
            _iocContainer.Resolve<ICompositionRoot>();
            _iocContainer.Resolve<ICompositionRoot>();
            _iocContainer.Resolve<ICompositionRoot>();
            _iocContainer.Resolve<ICompositionRoot>();
            _iocContainer.Resolve<ICompositionRoot>();
            _iocContainer.Resolve<ICompositionRoot>();
        }

        [Benchmark(Description = "IoC.Container composition root", OperationsPerInvoke = 10)]
        // ReSharper disable once InconsistentNaming
        public void IoCContainerByCR()
        {
            _iocRootResolver();
            _iocRootResolver();
            _iocRootResolver();
            _iocRootResolver();
            _iocRootResolver();
            _iocRootResolver();
            _iocRootResolver();
            _iocRootResolver();
            _iocRootResolver();
            _iocRootResolver();
        }

        [Benchmark]
        public void Autofac() => _autofacContainer.Resolve<ICompositionRoot>();

        [Benchmark]
        public void CastleWindsor() => _windsorContainerContainer.Resolve<ICompositionRoot>();

        [Benchmark(OperationsPerInvoke = 10)]
        public void DryIoc()
        {
            _dryIocContainer.Resolve<ICompositionRoot>();
            _dryIocContainer.Resolve<ICompositionRoot>();
            _dryIocContainer.Resolve<ICompositionRoot>();
            _dryIocContainer.Resolve<ICompositionRoot>();
            _dryIocContainer.Resolve<ICompositionRoot>();
            _dryIocContainer.Resolve<ICompositionRoot>();
            _dryIocContainer.Resolve<ICompositionRoot>();
            _dryIocContainer.Resolve<ICompositionRoot>();
            _dryIocContainer.Resolve<ICompositionRoot>();
            _dryIocContainer.Resolve<ICompositionRoot>();
        }

        [Benchmark(OperationsPerInvoke = 10)]
        public void SimpleInjector()
        {
            _simpleInjectorContainer.GetInstance<ICompositionRoot>();
            _simpleInjectorContainer.GetInstance<ICompositionRoot>();
            _simpleInjectorContainer.GetInstance<ICompositionRoot>();
            _simpleInjectorContainer.GetInstance<ICompositionRoot>();
            _simpleInjectorContainer.GetInstance<ICompositionRoot>();
            _simpleInjectorContainer.GetInstance<ICompositionRoot>();
            _simpleInjectorContainer.GetInstance<ICompositionRoot>();
            _simpleInjectorContainer.GetInstance<ICompositionRoot>();
            _simpleInjectorContainer.GetInstance<ICompositionRoot>();
            _simpleInjectorContainer.GetInstance<ICompositionRoot>();
        }

        [Benchmark(OperationsPerInvoke = 10)]
        public void LightInject()
        {
            _lightInjectContainer.GetInstance<ICompositionRoot>();
            _lightInjectContainer.GetInstance<ICompositionRoot>();
            _lightInjectContainer.GetInstance<ICompositionRoot>();
            _lightInjectContainer.GetInstance<ICompositionRoot>();
            _lightInjectContainer.GetInstance<ICompositionRoot>();
            _lightInjectContainer.GetInstance<ICompositionRoot>();
            _lightInjectContainer.GetInstance<ICompositionRoot>();
            _lightInjectContainer.GetInstance<ICompositionRoot>();
            _lightInjectContainer.GetInstance<ICompositionRoot>();
            _lightInjectContainer.GetInstance<ICompositionRoot>();
        }

        [Benchmark]
        public void Ninject() => _ninjectContainer.Get<ICompositionRoot>();

        [Benchmark]
        public void Unity() => _unityContainer.Resolve<ICompositionRoot>();

        [Benchmark(OperationsPerInvoke = 10)]
        public void MicrosoftDependencyInjection()
        {
            _microsoftContainer.GetService<ICompositionRoot>();
            _microsoftContainer.GetService<ICompositionRoot>();
            _microsoftContainer.GetService<ICompositionRoot>();
            _microsoftContainer.GetService<ICompositionRoot>();
            _microsoftContainer.GetService<ICompositionRoot>();
            _microsoftContainer.GetService<ICompositionRoot>();
            _microsoftContainer.GetService<ICompositionRoot>();
            _microsoftContainer.GetService<ICompositionRoot>();
            _microsoftContainer.GetService<ICompositionRoot>();
            _microsoftContainer.GetService<ICompositionRoot>();
        }
    }
}
