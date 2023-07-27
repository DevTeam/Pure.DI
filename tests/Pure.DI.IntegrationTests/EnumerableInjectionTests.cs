namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class EnumerableInjectionTests
{
    [Fact]
    public async Task ShouldSupportEnumerableInjection()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency
    {        
        public Dependency()
        {
            Console.WriteLine("Dependency created");
        }
    }

    interface IService
    {                    
    }

    class Service: IService 
    {
        public Service(System.Collections.Generic.IEnumerable<IDependency> deps)
        { 
            Console.WriteLine("Service creating");
            foreach (var dep in deps)
            {
            }
        }                            
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>(1).To<Dependency>()
                .Bind<IDependency>(2).To<Dependency>()
                .Bind<IDependency>(3).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Service;                                                     
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Service creating", "Dependency created", "Dependency created", "Dependency created"), result);
    }
    
    [Fact]
    public async Task ShouldOverrideDefaultEnumerableInjection()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using System.Collections.Generic;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency
    {        
        public Dependency()
        {
            Console.WriteLine("Dependency created");
        }
    }

    interface IService
    {                    
    }

    class Service: IService 
    {
        public Service(IEnumerable<IDependency> deps)
        { 
            Console.WriteLine("Service creating");
            foreach (var dep in deps)
            {
            }
        }                            
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>(1).To<Dependency>()
                .Bind<IDependency>(2).To<Dependency>()
                .Bind<IDependency>(3).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Bind<IEnumerable<IDependency>>().To(ctx => 
                {
                    ctx.Inject<IDependency>(1, out var dep1);
                    ctx.Inject<IDependency>(3, out var dep2);
                    return new List<IDependency> { dep1, dep2 };
                })
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Service;                                                     
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency created", "Dependency created", "Service creating"), result);
    }
    
    [Fact]
    public async Task ShouldProvideEmptyEnumerableWhenHasNoBindings()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency
    {        
        public Dependency()
        {
            Console.WriteLine("Dependency created");
        }
    }

    interface IService
    {                    
    }

    class Service: IService 
    {
        public Service(System.Collections.Generic.IEnumerable<IDependency> deps)
        { 
            Console.WriteLine("Service creating");
            foreach (var dep in deps)
            {
            }
        }                            
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IService>().To<Service>()
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Service;                                                     
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Service creating"), result);
    }
    
    [Fact]
    public async Task ShouldSupportEnumerableWhenPerResolve()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency
    {        
        public Dependency()
        {
            Console.WriteLine("Dependency created");
        }
    }

    interface IService
    {                    
    }

    class Service: IService 
    {
        public Service(System.Collections.Generic.IEnumerable<IDependency> deps)
        { 
            Console.WriteLine("Service creating");
            foreach (var dep in deps)
            {
            }
        }                            
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>(1).To<Dependency>()
                .Bind<IDependency>(2).As(Lifetime.PerResolve).To<Dependency>()
                .Bind<IDependency>(3).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Service;                                                     
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency created", "Service creating", "Dependency created", "Dependency created"), result);
    }
}