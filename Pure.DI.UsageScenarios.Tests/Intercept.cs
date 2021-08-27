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

    public class Intercept
    {
        [Fact]
        // $visible=true
        // $tag=5 Interception
        // $priority=02
        // $description=Intercept specific types
        // {
        public void Run()
        {
            DI.Setup()
                // Generates proxies
                .Bind<IProxyGenerator>().As(Singleton).To<ProxyGenerator>()
                // Controls creating instances of type Dependency
                .Bind<IFactory<Dependency>>().As(Singleton).To<MyInterceptor<Dependency>>()
                // Controls creating instances of type Service
                .Bind<IFactory<Service>>().As(Singleton).To<MyInterceptor<Service>>()

                .Bind<IDependency>().As(Singleton).To<Dependency>()
                .Bind<IService>().To<Service>();
            
            var instance = InterceptDI.Resolve<IService>();
            instance.Run();
            instance.Run();
            instance.Run();

            // Check number of invocations
            InterceptDI.Resolve<MyInterceptor<Service>>().InvocationCounter.ShouldBe(3);
            InterceptDI.Resolve<MyInterceptor<Dependency>>().InvocationCounter.ShouldBe(3);
        }

        public class MyInterceptor<T>: IFactory<T>, IInterceptor
            where T: class
        {
            private readonly IProxyGenerator _proxyGenerator;

            public MyInterceptor(IProxyGenerator proxyGenerator) =>
                _proxyGenerator = proxyGenerator;

            public int InvocationCounter { get; private set; }

            public T Create(Func<T> factory) => 
                (T)_proxyGenerator.CreateClassProxyWithTarget(
                    typeof(T),
                    typeof(T).GetInterfaces(),
                    factory(),
                    this);

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
