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
    
    [Fact]
    public async Task ShouldSupportEnumerableWhenPerResolveInFunc()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency
                               {
                                    int Id { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   private static int _nextId;
                                   public Dependency()
                                   {
                                       Id = _nextId++;
                                       Console.WriteLine($"Dependency {_nextId} created");
                                   }
                                   
                                   public int Id { get; set; }
                               }
                           
                               interface IService
                               {
                               }
                           
                               class Service: IService
                               {
                                   public Service(IEnumerable<int> deps)
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
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>(1).To<Dependency>()
                                           .Bind<IDependency>(2).As(Lifetime.PerResolve).To<Dependency>()
                                           .Bind<IDependency>(3).To<Dependency>()
                                           .Bind<IEnumerable<int>>().To(ctx => 
                                           {
                                                ctx.Inject(out IEnumerable<IDependency> dependencies);
                                                return dependencies.Select(i => i.Id);
                                           })
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
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency 1 created", "Service creating", "Dependency 2 created", "Dependency 3 created"), result);
    }
}