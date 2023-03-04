namespace Pure.DI.IntegrationTests;

using System.Collections.Immutable;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class FuncTests
{
    [Fact]
    public async Task ShouldSupportFuncForTransientDependencies()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency {}

    interface IService
    {
        IDependency Dep { get; }        
    }

    class Service: IService 
    {
        private Func<IDependency> _depFactory;
        public Service(Func<IDependency> depFactory)
        { 
            _depFactory = depFactory;           
        }

        public IDependency Dep => _depFactory();
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()    
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer();
            var service = composer.Service;
            Console.WriteLine(service.Dep != service.Dep);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportFuncForSingletonDependencies()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency {}

    interface IService
    {
        IDependency Dep { get; }        
    }

    class Service: IService 
    {
        private Func<IDependency> _depFactory;
        public Service(Func<IDependency> depFactory)
        { 
            _depFactory = depFactory;           
        }

        public IDependency Dep => _depFactory();
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()    
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer();
            var service = composer.Service;
            Console.WriteLine(service.Dep == service.Dep);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportFuncForPerResolveDependencies()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency {}

    interface IService
    {
        IDependency Dep { get; }        
    }

    class Service: IService 
    {
        private Func<IDependency> _depFactory;
        public Service(Func<IDependency> depFactory)
        { 
            _depFactory = depFactory;           
        }

        public IDependency Dep => _depFactory();
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
                .Bind<IService>().To<Service>()    
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer();
            var service = composer.Service;
            Console.WriteLine(service.Dep == service.Dep);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result.GeneratedCode);
    }
}