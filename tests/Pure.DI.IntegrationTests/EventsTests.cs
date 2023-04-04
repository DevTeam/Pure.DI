namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class EventsTests
{
    [Fact]
    public async Task ShouldTrackInstanceCreated()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    internal interface IDependency { }

    internal class Dependency : IDependency { }

    internal interface IService
    {
        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal class Service : IService
    {
        public Service(IDependency dependency1, Func<IDependency> dependencyFactory)
        {
            Dependency1 = dependency1;
            Dependency2 = dependencyFactory();
        }

        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal partial class Composition
    {
        partial void OnInstanceCreation<T>(ref T value, object? tag, object? lifetime)            
        {
            Console.WriteLine($"{typeof(T)} '{tag}' {lifetime} created");            
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Root");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Root;            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.Dependency '' Singleton created", "System.Func`1[Sample.IDependency] ''  created", "Sample.Service ''  created"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldTrackInstanceInjected()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    internal interface IDependency { }

    internal class Dependency : IDependency { }

    internal interface IService
    {
        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal class Service : IService
    {
        public Service(IDependency dependency1, Func<IDependency> dependencyFactory)
        {
            Dependency1 = dependency1;
            Dependency2 = dependencyFactory();
        }

        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal partial class Composition
    {
        private partial T OnDependencyInjection<T>(in T value, object? tag, object? lifetime)            
        {
            Console.WriteLine($"{typeof(T)} '{tag}' {lifetime} injected");
            return value;                  
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnDependencyInjection = On
            DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Root");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Root;            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.IDependency '' Singleton injected", "System.Func`1[Sample.IDependency] ''  injected", "Sample.IDependency '' Singleton injected", "Sample.IService ''  injected"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldTrackInstanceInjectedWhenFilterByImplementationType()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    internal interface IDependency { }

    internal class Dependency : IDependency { }

    internal interface IService
    {
        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal class Service : IService
    {
        public Service(IDependency dependency1, Func<IDependency> dependencyFactory)
        {
            Dependency1 = dependency1;
            Dependency2 = dependencyFactory();
        }

        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal partial class Composition
    {
        private partial T OnDependencyInjection<T>(in T value, object? tag, object? lifetime)            
        {
            Console.WriteLine($"{typeof(T)} '{tag}' {lifetime} injected");
            return value;                  
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnDependencyInjection = On
            // OnDependencyInjectionImplementationTypeNameRegularExpression = \.Dependency
            DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Root");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Root;            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.IDependency '' Singleton injected", "Sample.IDependency '' Singleton injected"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldTrackInstanceInjectedWhenFilterByContractType()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    internal interface IDependency { }

    internal class Dependency : IDependency { }

    internal interface IService
    {
        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal class Service : IService
    {
        public Service(IDependency dependency1, Func<IDependency> dependencyFactory)
        {
            Dependency1 = dependency1;
            Dependency2 = dependencyFactory();
        }

        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal partial class Composition
    {
        private partial T OnDependencyInjection<T>(in T value, object? tag, object? lifetime)            
        {
            Console.WriteLine($"{typeof(T)} '{tag}' {lifetime} injected");
            return value;                  
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnDependencyInjection = On
            // OnDependencyInjectionContractTypeNameRegularExpression = \.IService
            DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Root");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Root;            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.IService ''  injected"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldTrackInstanceInjectedWhenFilterByTag()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    internal interface IDependency { }

    internal class Dependency : IDependency { }

    internal interface IService
    {
        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal class Service : IService
    {
        public Service([Tag("Abc")] IDependency dependency1, [Tag("Abc")] IDependency dependency2)
        {
            Dependency1 = dependency1;
            Dependency2 = dependency2;
        }

        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal partial class Composition
    {
        private partial T OnDependencyInjection<T>(in T value, object? tag, object? lifetime)            
        {
            Console.WriteLine($"{typeof(T)} '{tag}' {lifetime} injected");
            return value;                  
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnDependencyInjection = On
            // OnDependencyInjectionTagRegularExpression = \"Abc\"
            DI.Setup("Composition")
                .Bind<IDependency>("Abc").As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Root");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Root;            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.IDependency 'Abc' Singleton injected", "Sample.IDependency 'Abc' Singleton injected"), result.GeneratedCode);
    }
}