// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageScenarios.Tests
{
    using System;
    using Castle.DynamicProxy;
    using Shouldly;
    using Xunit;
    using static Lifetime;

    public class InterceptMany
    {
        [Fact]
        // $visible=true
        // $tag=5 Interception
        // $priority=03
        // $description=Intercept a set of types
        // {
        public void Run()
        {
            DI.Setup()
                // Generates proxies
                .Bind<IProxyGenerator>().As(Singleton).To<ProxyGenerator>()
                // Controls creating instances
                .Bind<IFactory>().As(Singleton).To<MyInterceptor>()

                .Bind<IDependency>().As(Singleton).To<Dependency>()
                .Bind<IService>().To<Service>();
            
            var instance = InterceptManyDI.Resolve<IService>();
            instance.Run();
            instance.Run();

            // Check number of invocations
            InterceptManyDI.Resolve<MyInterceptor>().InvocationCounter.ShouldBe(4);
        }

        [Exclude(nameof(ProxyGenerator))]
        public class MyInterceptor: IFactory, IInterceptor
        {
            private readonly IProxyGenerator _proxyGenerator;

            public MyInterceptor(IProxyGenerator proxyGenerator) =>
                _proxyGenerator = proxyGenerator;
            
            public int InvocationCounter { get; private set; }

            public T Create<T>(Func<T> factory, object tag) => 
                (T)_proxyGenerator.CreateClassProxyWithTarget(typeof(T), typeof(T).GetInterfaces(), factory(), this);

            void IInterceptor.Intercept(IInvocation invocation)
            {
                InvocationCounter++;
                invocation.Proceed();
            }
        }
        
        public interface IDependency { void Run();}

        public class Dependency : IDependency { public void Run() {} }

        public interface IService { void Run(); }

        public class Service : IService
        {
            private readonly IDependency? _dependency;

            public Service() { }

            public Service(IDependency dependency) { _dependency = dependency; }

            public void Run() { _dependency?.Run(); }
        }
        // }
    }
}