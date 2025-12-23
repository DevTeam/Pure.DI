namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
/// Tests related to the injection of IEnumerable dependencies.
/// </summary>
public class EnumerableInjectionTests
{
    [Theory]
    [InlineData(LanguageVersion.CSharp8)]
    [InlineData(LanguageVersion.CSharp9)]
    [InlineData(LanguageVersion.CSharp10)]
    public async Task ShouldSupportEnumerableInjection(LanguageVersion languageVersion)
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
                                   public Service(System.Collections.Generic.IEnumerable<IDependency> deps, System.Collections.Generic.IEnumerable<IDependency> deps2)
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
                           """.RunAsync(new Options(languageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Service creating", "Dependency created", "Dependency created", "Dependency created"], result);
        result.GeneratedCode.Contains(Names.PerBlockVariablePrefix).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableInjectionOptimization()
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
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Service creating", "Dependency created", "Dependency created", "Dependency created"], result);
        result.GeneratedCode.Contains(Names.PerBlockVariablePrefix).ShouldBeFalse(result);
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
                                       // FormatCode = On;
                                       DI.Setup("Composition")
                                           .Bind<IDependency>(1).To<Dependency>()
                                           .Bind<IDependency>(2).To<Dependency>()
                                           .Bind<IDependency>(3).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Bind<IEnumerable<IDependency>>().To(ctx => 
                                           {
                                               ctx.Inject<IDependency>(1, out var dep1);
                                               ctx.Inject<IDependency>(2, out var dep2);
                                               ctx.Inject<IDependency>(3, out var dep3);
                                               return new List<IDependency> { dep1, dep3 };
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
        result.StdOut.ShouldBe(["Service creating"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableInjectionWhenGeneric()
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
                                   public Service(System.Collections.Generic.IEnumerable<IDependency<string>> deps)
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
                                           .Bind<IDependency<TT>>(1).To<Dependency<TT>>()
                                           .Bind<IDependency<TT>>(2).To<Dependency<TT>>()
                                           .Bind<IDependency<TT>>(3).To<Dependency<TT>>()
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
        result.StdOut.ShouldBe(["Service creating", "Dependency created", "Dependency created", "Dependency created"], result);
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
        result.StdOut.ShouldBe(["Service creating", "Dependency created", "Dependency created", "Dependency created"], result);
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
        result.StdOut.ShouldBe(["Service creating", "Dependency 1 created", "Dependency 2 created", "Dependency 3 created"], result);
    }
    [Fact]
    public async Task ShouldSupportEnumerableOfValueTypes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               struct Dependency {}
                           
                               class Service
                               {
                                   public Service(IEnumerable<Dependency> deps)
                                   { 
                                       int count = 0;
                                       foreach(var dep in deps) count++;
                                       Console.WriteLine($"Count: {count}");
                                   }    
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Dependency>(1).To<Dependency>()
                                           .Bind<Dependency>(2).To<Dependency>()
                                           .Root<Service>("Root");
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Count: 2"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableWithMixedLifetimes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency1: IDependency {}
                               class Dependency2: IDependency {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>(1).As(Lifetime.Singleton).To<Dependency1>()
                                           .Bind<IDependency>(2).As(Lifetime.Transient).To<Dependency2>()
                                           .Root<IEnumerable<IDependency>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root1 = composition.Root;
                                       var root2 = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportNestedEnumerable()
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
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {        
                                   public Dependency() => Console.WriteLine("Dependency created");
                               }
                           
                               interface IService {}
                           
                               class Service: IService 
                               {
                                   public Service(IEnumerable<IEnumerable<IDependency>> deps)
                                   { 
                                       Console.WriteLine("Service creating");
                                       foreach (var dep in deps.SelectMany(i => i))
                                       {
                                       }
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IEnumerable<IDependency>>().To(ctx => 
                                           {
                                               ctx.Inject<IDependency>(out var dep);
                                               return new List<IDependency> { dep };
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
        result.StdOut.ShouldBe(["Service creating", "Dependency created"], result);
    }

    [Fact]
    public async Task ShouldSupportTaggedEnumerable()
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
                               interface IDependency {}
                           
                               class Dependency1: IDependency {}
                               class Dependency2: IDependency {}
                           
                               interface IService {}
                           
                               class Service: IService 
                               {
                                   public Service([Tag("SomeTag")] IEnumerable<IDependency> deps)
                                   { 
                                       Console.WriteLine($"Count: {deps.Count()}");
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>("SomeTag").To<Dependency1>()
                                           .Bind<IDependency>("OtherTag").To<Dependency2>()
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
        result.StdOut.ShouldBe(["Count: 2"], result);
    }

    [Theory]
    [InlineData("IReadOnlyCollection")]
    [InlineData("IReadOnlyList")]
    [InlineData("ICollection")]
    [InlineData("IList")]
    public async Task ShouldSupportCollectionInjections(string collectionType)
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
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService {}
                           
                               class Service: IService 
                               {
                                   public Service(###CollectionType###<IDependency> deps)
                                   { 
                                       Console.WriteLine($"Count: {deps.Count()}");
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>(1).To<Dependency>()
                                           .Bind<IDependency>(2).To<Dependency>()
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
                           """.Replace("###CollectionType###", collectionType).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Count: 2"], result);
    }

    [Fact]
    public async Task ShouldSupportArrayInjection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService {}
                           
                               class Service: IService 
                               {
                                   public Service(IDependency[] deps)
                                   { 
                                       Console.WriteLine($"Count: {deps.Length}");
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>(1).To<Dependency>()
                                           .Bind<IDependency>(2).To<Dependency>()
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
        result.StdOut.ShouldBe(["Count: 2"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableOfTask()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               class Service
                               {
                                   public Service(IEnumerable<Task<IDependency>> deps)
                                   { 
                                       Console.WriteLine($"Count: {deps.Count()}");
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Root");
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Count: 1"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableAsRoot()
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
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>(1).To<Dependency>()
                                           .Bind<IDependency>(2).To<Dependency>()
                                           .Root<IEnumerable<IDependency>>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine($"Count: {composition.Root.Count()}");                              
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Count: 2"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableOfGenericTypes()
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
                               interface IDependency<T> {}
                           
                               class Dependency<T>: IDependency<T> {}
                           
                               class Service
                               {
                                   public Service(IEnumerable<IDependency<int>> intDeps, IEnumerable<IDependency<string>> stringDeps)
                                   { 
                                       Console.WriteLine($"Ints: {intDeps.Count()}, Strings: {stringDeps.Count()}");
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Root");
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ints: 1, Strings: 1"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableWithMultipleContracts()
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
                               interface IContract1 {}
                               interface IContract2 {}
                           
                               class Dependency: IContract1, IContract2 {}
                           
                               class Service
                               {
                                   public Service(IEnumerable<IContract1> deps1, IEnumerable<IContract2> deps2)
                                   { 
                                       Console.WriteLine($"Deps1: {deps1.Count()}, Deps2: {deps2.Count()}");
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IContract1>().Bind<IContract2>().To<Dependency>()
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Root");
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Deps1: 1, Deps2: 1"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableWhenRecursive()
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
                               interface IDependency {}
                           
                               class Dependency: IDependency 
                               {
                                   public Dependency(IEnumerable<IService> services) 
                                   {
                                       Console.WriteLine($"Dependency created with {services.Count()} services");
                                   }
                               }
                           
                               interface IService {}
                           
                               class Service: IService 
                               {
                                   public Service() {}
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IEnumerable<IDependency>>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var deps = composition.Root.ToList();                              
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency created with 1 services"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableOfPrimitiveTypes()
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
                               class Service
                               {
                                   public Service(IEnumerable<int> ints, IEnumerable<string> strings)
                                   { 
                                       Console.WriteLine($"Ints: {string.Join(",", ints)}, Strings: {string.Join(",", strings)}");
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<int>(1).To(_ => 1)
                                           .Bind<int>(2).To(_ => 2)
                                           .Bind<string>(1).To(_ => "A")
                                           .Bind<string>(2).To(_ => "B")
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Root");
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ints: 1,2, Strings: A,B"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableInGenericClass()
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
                               interface IDependency {}
                               class Dependency: IDependency {}
                           
                               class Service<T>
                               {
                                   public Service(IEnumerable<IDependency> deps)
                                   { 
                                       Console.WriteLine($"Deps: {deps.Count()}");
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<Service<TT>>().To<Service<TT>>()
                                           .Root<Service<int>>("Root");
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Deps: 1"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableWithDefaultValuesInCtor()
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
                               interface IDependency {}
                           
                               class Service
                               {
                                   public Service(IEnumerable<IDependency>? deps = null)
                                   { 
                                       Console.WriteLine($"Deps is null: {deps == null}");
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Root");
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
        result.Success.ShouldBeTrue(result);
        // Pure.DI should provide an empty enumerable even if the default value is null, 
        // because it knows how to create an enumerable.
        result.StdOut.ShouldBe(["Deps is null: False"], result);
    }

    [Fact]
    public async Task ShouldSupportEnumerableOfFuncWithArgs()
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
                                   string Name { get; }
                               }
                           
                               class Dependency: IDependency 
                               {
                                   public Dependency(string name) => Name = name;
                                   public string Name { get; }
                               }
                           
                               class Service
                               {
                                   public Service(IEnumerable<Func<string, IDependency>> factories)
                                   { 
                                       var deps = factories.Select(f => f("Test")).ToList();
                                       Console.WriteLine($"Count: {deps.Count}, Name: {deps[0].Name}");
                                   }    
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Root");
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Count: 1, Name: Test"], result);
    }
}