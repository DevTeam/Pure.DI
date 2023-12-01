// ReSharper disable RedundantUsingDirective
// ReSharper disable UnusedMember.Global
#pragma warning disable CS8601
#pragma warning disable CS0649
#pragma warning disable CS8618
#pragma warning disable CS8604
#pragma warning disable CS8602
#pragma warning disable CS0169
namespace Pure.DI.Benchmarks;

using System.Diagnostics.CodeAnalysis;
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
using Container = IoC.Container;
using ICompositionRoot = Model.ICompositionRoot;
using IContainer = Autofac.IContainer;

public abstract class BenchmarkBase
{
    private Container _iocContainer;
    private Func<ICompositionRoot> _iocRootResolver;
    private IContainer _autofacContainer;
    private WindsorContainer _windsorContainerContainer;
    private global::DryIoc.Container _dryIocContainer;
    private ServiceContainer _lightInjectContainer;
    private StandardKernel _ninjectContainer;
    private UnityContainer _unityContainer;
    private ServiceProvider _microsoftContainer;
    private global::SimpleInjector.Container? _simpleInjectorContainer;

    protected abstract TContainer? CreateContainer<TContainer, TAbstractContainer>()
        where TAbstractContainer : IAbstractContainer<TContainer>, new() where TContainer : class;

    [GlobalSetup]
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public void Setup()
    {
#if !DEBUG
        _autofacContainer = CreateContainer<IContainer, Autofac>();
        _dryIocContainer = CreateContainer<global::DryIoc.Container, DryIoc>();
        _lightInjectContainer = CreateContainer<ServiceContainer, LightInject>();
        _microsoftContainer = CreateContainer<ServiceProvider, MicrosoftDependencyInjection>();
        _simpleInjectorContainer = CreateContainer<global::SimpleInjector.Container, SimpleInjector>();
        _windsorContainerContainer = CreateContainer<WindsorContainer, CastleWindsor>();
        _ninjectContainer = CreateContainer<StandardKernel, Ninject>();
        _unityContainer = CreateContainer<UnityContainer, Unity>();
#if LEGACY
        _iocContainer = CreateContainer<Container, IoCContainer>();
        _iocRootResolver = CreateContainer<Func<ICompositionRoot>, IoCContainerByCompositionRoot<ICompositionRoot>>();
#endif
#endif
    } 

#if !DEBUG
    [Benchmark]
    public ICompositionRoot Autofac() => _autofacContainer.Resolve<ICompositionRoot>();
    
    [Benchmark]
    public ICompositionRoot DryIoc() => _dryIocContainer.Resolve<ICompositionRoot>();

    [Benchmark(Description = "Simple Injector")]
    public ICompositionRoot SimpleInjector() => _simpleInjectorContainer.GetInstance<ICompositionRoot>();

    [Benchmark]
    public ICompositionRoot LightInject() => _lightInjectContainer.GetInstance<ICompositionRoot>();
    
    [Benchmark(Description = "Microsoft DI")]
    public object? MicrosoftDependencyInjection() => _microsoftContainer.GetService<ICompositionRoot>();
    
    [Benchmark(Description = "Castle Windsor")]
    public ICompositionRoot CastleWindsor() => _windsorContainerContainer.Resolve<ICompositionRoot>();

    [Benchmark]
    public ICompositionRoot Ninject() => _ninjectContainer.Get<ICompositionRoot>();

    [Benchmark]
    public ICompositionRoot Unity() => _unityContainer.Resolve<ICompositionRoot>();
    
#if LEGACY
    [Benchmark(Description = "IoC.Container")]
    public ICompositionRoot IoCContainer() => _iocContainer.Resolve<ICompositionRoot>();

    [Benchmark(Description = "IoC.Container composition root")]
    // ReSharper disable once InconsistentNaming
    public ICompositionRoot IoCContainerByCR() => _iocRootResolver();
#endif
#endif
}