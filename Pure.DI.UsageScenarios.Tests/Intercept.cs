// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageScenarios.Tests;

using Castle.DynamicProxy;
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
            .Default(Singleton)
            // Generates proxies
            .Bind<IProxyGenerator>().To<ProxyGenerator>()
            // Controls creating instances of type Dependency
            .Bind<IFactory<IDependency>>().To<MyInterceptor<IDependency>>()
            // Controls creating instances of type Service
            .Bind<IFactory<IService>>().To<MyInterceptor<IService>>()
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().As(Transient).To<Service>();

        var instance = InterceptDI.Resolve<IService>();
        instance.Run();
        instance.Run();
        instance.Run();

        // Check number of invocations
        ((MyInterceptor<IService>)InterceptDI.Resolve<IFactory<IService>>()).InvocationCounter.ShouldBe(3);
        ((MyInterceptor<IDependency>)InterceptDI.Resolve<IFactory<IDependency>>()).InvocationCounter.ShouldBe(3);
    }

    public class MyInterceptor<T> : IFactory<T>, IInterceptor
        where T : class
    {
        private readonly IProxyGenerator _proxyGenerator;

        public MyInterceptor(IProxyGenerator proxyGenerator) =>
            _proxyGenerator = proxyGenerator;

        public int InvocationCounter { get; private set; }

        public T Create(Func<T> factory, Type implementationType, object tag) =>
            _proxyGenerator.CreateInterfaceProxyWithTarget(factory(), this);

        void IInterceptor.Intercept(IInvocation invocation)
        {
            InvocationCounter++;
            invocation.Proceed();
        }
    }

    public interface IDependency
    {
        void Run();
    }

    public class Dependency : IDependency
    {
        public void Run() { }
    }

    public interface IService
    {
        void Run();
    }

    public class Service : IService
    {
        private readonly IDependency? _dependency;

        public Service(IDependency dependency) { _dependency = dependency; }

        public void Run() { _dependency?.Run(); }
    }
    // }
}