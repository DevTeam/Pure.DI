namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class PartialMethodsTests
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
        partial void OnInstanceCreation<T>(ref T value, object? tag, Lifetime lifetime)            
        {
            Console.WriteLine($"{typeof(T)} '{tag}' {lifetime} created");            
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnInstanceCreation = On
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
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.Dependency '' Singleton created", "System.Func`1[Sample.IDependency] '' Transient created", "Sample.Service '' Transient created"), result.GeneratedCode);
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
        private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)            
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
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.IDependency '' Singleton injected", "System.Func`1[Sample.IDependency] '' Transient injected", "Sample.IDependency '' Singleton injected", "Sample.IService '' Transient injected"), result.GeneratedCode);
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
        private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)            
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
        private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)            
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
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.IService '' Transient injected"), result.GeneratedCode);
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
        private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)            
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
    
    [Fact]
    public async Task ShouldSupportOnCannotResolve()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    internal interface IDependency { }

    internal class Dependency : IDependency
    { 
        public Dependency([Tag("some ID")] int id)
        {
            Console.WriteLine($"Dependency {id} created");
        }
    }

    internal interface IService
    {
        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal class Service : IService
    {
        public Service(string name, IDependency dependency1, Func<IDependency> dependencyFactory)
        {
            Dependency1 = dependency1;
            Dependency2 = dependencyFactory();
            Console.WriteLine($"Service '{name}' created");
        }

        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal partial class Composition
    {
        private partial T OnCannotResolve<T>(object? tag, Lifetime lifetime)            
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)"MyService";
            }            

            if (typeof(T) == typeof(int) && Equals(tag, "some ID"))
            {
                return (T)(object)99;
            }
            
            throw new Exception("Cannot resolve."); 
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnCannotResolve = On
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
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency 99 created", "Service 'MyService' created"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportOnCannotResolveWhenFilterByContract()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    internal interface IDependency { }

    internal class Dependency : IDependency
    { 
        public Dependency([Tag("some ID")] int id)
        {
            Console.WriteLine($"Dependency {id} created");
        }
    }

    internal interface IService
    {
        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal class Service : IService
    {
        public Service(string name, IDependency dependency1, Func<IDependency> dependencyFactory)
        {
            Dependency1 = dependency1;
            Dependency2 = dependencyFactory();
            Console.WriteLine($"Service '{name}' created");
        }

        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal partial class Composition
    {
        private partial T OnCannotResolve<T>(object? tag, Lifetime lifetime)            
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)"MyService";
            }            

            if (typeof(T) == typeof(int) && Equals(tag, "some ID"))
            {
                return (T)(object)99;
            }
            
            throw new Exception("Cannot resolve."); 
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnCannotResolve = On
            // OnCannotResolveContractTypeNameRegularExpression = int
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
        result.Success.ShouldBeFalse(result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportOnCannotResolveWhenFilterByTag()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    internal interface IDependency { }

    internal class Dependency : IDependency
    { 
        public Dependency([Tag("some ID")] int id)
        {
            Console.WriteLine($"Dependency {id} created");
        }
    }

    internal interface IService
    {
        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal class Service : IService
    {
        public Service(string name, IDependency dependency1, Func<IDependency> dependencyFactory)
        {
            Dependency1 = dependency1;
            Dependency2 = dependencyFactory();
            Console.WriteLine($"Service '{name}' created");
        }

        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal partial class Composition
    {
        private partial T OnCannotResolve<T>(object? tag, Lifetime lifetime)            
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)"MyService";
            }            

            if (typeof(T) == typeof(int) && Equals(tag, "some ID"))
            {
                return (T)(object)99;
            }
            
            throw new Exception("Cannot resolve."); 
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnCannotResolve = On
            // OnCannotResolveTagRegularExpression = null
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
        result.Success.ShouldBeFalse(result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportOnCannotResolveWhenGeneric()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using System.Collections.Generic;

namespace Sample
{
    internal interface IDependency<T> { }

    internal class Dependency<T> : IDependency<T>
    {         
    }

    internal interface IService<T>
    {
    }

    internal class Service<T> : IService<T>
    {
        public Service(IDependency<T> dependency)
        {
            Console.WriteLine($"Service with {dependency} created");
        }        
    }

    internal partial class Composition
    {
        private partial T OnCannotResolve<T>(object? tag, Lifetime lifetime)            
        {
            if (typeof(T) == typeof(IDependency<string>))
            {
                return (T)(object)new Dependency<string>();
            }            

            throw new Exception("Cannot resolve."); 
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnCannotResolve = On
            DI.Setup("Composition")
                .Bind<IService<TT>>().To<Service<TT>>()
                .Root<IService<string>>("Root");
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
        result.StdOut.ShouldBe(ImmutableArray.Create("Service with Sample.Dependency`1[System.String] created"), result.GeneratedCode);
    }
}