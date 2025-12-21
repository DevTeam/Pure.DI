namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the injection of arrays.
/// </summary>
public class ArrayInjectionTests
{

    [Fact]
    public async Task ShouldOverrideDefaultArrayInjection()
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
                                   public Service(IDependency[] deps)
                                   { 
                                       Console.WriteLine("Service creating");
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
                                           .Bind<IDependency[]>().To(ctx => 
                                           {
                                               ctx.Inject<IDependency>(1, out var dep1);
                                               ctx.Inject<IDependency>(2, out var dep2);
                                               ctx.Inject<IDependency>(3, out var dep3);
                                               return new IDependency[] { dep1, dep2, dep3 };
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
        result.StdOut.ShouldBe(["Dependency created", "Dependency created", "Dependency created", "Service creating"], result);
    }
    [Fact]
    public async Task ShouldSupportArrayInjection()
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
                                   public Service(IDependency[] deps)
                                   { 
                                       Console.WriteLine("Service creating");
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
        result.StdOut.ShouldBe(["Dependency created", "Dependency created", "Dependency created", "Service creating"], result);
    }

    [Fact]
    public async Task ShouldProvideEmptyArrayWhenHasNoBindings()
    {
        // Given
        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}

                               interface IService
                               {
                                   IDependency[] Deps { get; }
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency[] deps)
                                   { 
                                       Deps = deps;
                                   }
                                   
                                   public IDependency[] Deps { get; }
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
                                       Console.WriteLine(service.Deps.Length);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["0"], result);
    }

    [Fact]
    public async Task ShouldSupportArrayInjectionWhenGeneric()
    {
        // Given
        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}

                               class Dependency<T>: IDependency<T>
                               {
                                   public override string ToString() => typeof(T).Name;
                               }

                               interface IService
                               {
                                   void Run();
                               }

                               class Service<T>: IService 
                               {
                                   public Service(IDependency<T>[] deps)
                                   {
                                       foreach(var dep in deps)
                                       {
                                           Console.WriteLine(dep);
                                       }
                                   }
                                   
                                   public void Run() {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Bind<IService>().To<Service<string>>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       service.Run();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["String"], result);
    }

    [Fact]
    public async Task ShouldSupportArrayInjectionWhenPerResolve()
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
                                   public Dependency() => Console.WriteLine("Dependency created");
                               }

                               interface IService
                               {
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency[] deps, IDependency dep)
                                   {
                                       Console.WriteLine($"Equals: {ReferenceEquals(deps[0], dep)}");
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
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
        result.StdOut.ShouldBe(["Dependency created", "Equals: True"], result);
    }

    [Fact]
    public async Task ShouldSupportArrayInjectionWhenSingleton()
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
                                   public Dependency() => Console.WriteLine("Dependency created");
                               }

                               interface IService
                               {
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency[] deps, IDependency dep)
                                   {
                                       Console.WriteLine($"Equals: {ReferenceEquals(deps[0], dep)}");
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Service;
                                       var service2 = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency created", "Equals: True", "Equals: True"], result);
    }

    [Fact]
    public async Task ShouldSupportArrayOfFuncInjection()
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
                                   public Dependency() => Console.WriteLine("Dependency created");
                               }

                               interface IService
                               {
                               }

                               class Service: IService 
                               {
                                   public Service(Func<IDependency>[] deps)
                                   {
                                       Console.WriteLine("Service creating");
                                       foreach(var dep in deps)
                                       {
                                           dep();
                                       }
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
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
        result.StdOut.ShouldBe(["Service creating", "Dependency created"], result);
    }
}