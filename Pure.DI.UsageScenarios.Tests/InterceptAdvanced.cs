// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Castle.DynamicProxy;
    using Shouldly;
    using Xunit;
    using static Lifetime;

    public class InterceptAdvanced
    {
        [Fact]
        // $visible=true
        // $tag=5 Interception
        // $priority=03
        // $description=Intercept advanced
        // $header=This approach represents a fastest way of working with interceptors.
        // {
        public void Run()
        {
            DI.Setup()
                .Default(Singleton)
                // Generates proxies
                .Bind<IProxyBuilder>().To<DefaultProxyBuilder>()
                // Controls creating instances of types Dependency and Service filtered by the [Include(...)] attribute
                .Bind<IFactory<TT>>().To<MyInterceptor<TT>>()
                
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().As(Transient).To<Service>();
            
            var instance = InterceptAdvancedDI.Resolve<IService>();
            instance.Run();
            instance.Run();
            instance.Run();

            // Check number of invocations
            ((MyInterceptor<IService>)InterceptAdvancedDI.Resolve<IFactory<IService>>()).InvocationCounter.ShouldBe(3);
            ((MyInterceptor<IDependency>)InterceptAdvancedDI.Resolve<IFactory<IDependency>>()).InvocationCounter.ShouldBe(3);
        }
        
        // Filters for Service and for Dependency classes
        [Include("(Service|Dependency)$")]
        internal class MyInterceptor<T>: IFactory<T>, IInterceptor
            where T: class
        {
            private readonly Func<T, T> _proxyFactory;

            public MyInterceptor(IProxyBuilder proxyBuilder) =>
                _proxyFactory = CreateProxyFactory(proxyBuilder, this);

            public int InvocationCounter { get; private set; }

            public T Create(Func<T> factory, Type implementationType, object tag, Lifetime lifetime) =>
                // Creates a proxy for an instance
                _proxyFactory(factory());

            void IInterceptor.Intercept(IInvocation invocation)
            {
                InvocationCounter++;
                invocation.Proceed();
            }
            
            // Compiles a delegate to create a proxy for the performance boost
            private static Func<T, T> CreateProxyFactory(IProxyBuilder proxyBuilder, params IInterceptor[] interceptors)
            {
                var proxyType = proxyBuilder.CreateInterfaceProxyTypeWithTargetInterface(typeof(T), Type.EmptyTypes, ProxyGenerationOptions.Default);
                var ctor = proxyType.GetConstructors().Single(i => i.GetParameters().Length == 2);
                var instance = Expression.Parameter(typeof(T));
                var newProxyExpression = Expression.New(ctor, Expression.Constant(interceptors), instance);
                return Expression.Lambda<Func<T, T>>(newProxyExpression, instance).Compile();
            }
        }

        public interface IDependency { void Run();}

        public class Dependency : IDependency { public void Run() {} }

        public interface IService { void Run(); }

        public class Service : IService
        {
            private readonly IDependency? _dependency;

            public Service(IDependency dependency) { _dependency = dependency; }

            public void Run() { _dependency?.Run(); }
        }
        // }
    }
}