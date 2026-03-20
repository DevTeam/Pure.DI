namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the injection of BCL types (e.g., IEnumerable, IAsyncEnumerable, Span, ReadOnlySpan, etc.).
/// </summary>
public class BclInjectionTests
{
    [Theory]
    [InlineData("System.Collections.Generic.IList")]
    [InlineData("System.Collections.Immutable.ImmutableArray")]
    [InlineData("System.Collections.Generic.IReadOnlyList")]
    [InlineData("System.Collections.Generic.ISet")]
    [InlineData("System.Collections.Generic.HashSet")]
    [InlineData("System.Collections.Generic.SortedSet")]
    [InlineData("System.Collections.Generic.Queue")]
    [InlineData("System.Collections.Generic.Stack")]
    [InlineData("System.Collections.Immutable.IImmutableList")]
    [InlineData("System.Collections.Immutable.ImmutableList")]
    [InlineData("System.Collections.Immutable.IImmutableSet")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet")]
    [InlineData("System.Collections.Immutable.ImmutableSortedSet")]
    [InlineData("System.Collections.Immutable.IImmutableQueue")]
    [InlineData("System.Collections.Immutable.ImmutableQueue")]
    [InlineData("System.Collections.Immutable.IImmutableStack")]
    [InlineData("System.Collections.Immutable.ImmutableStack")]
    [InlineData("System.Span")]
    [InlineData("System.Span", LanguageVersion.CSharp8)]
    [InlineData("System.ReadOnlySpan")]
    [InlineData("System.Memory")]
    [InlineData("System.ReadOnlyMemory")]
    [InlineData("System.Collections.Concurrent.IProducerConsumerCollection")]
    [InlineData("System.Collections.Concurrent.ConcurrentBag")]
    [InlineData("System.Collections.Concurrent.ConcurrentQueue")]
    [InlineData("System.Collections.Concurrent.ConcurrentStack")]
    [InlineData("System.Collections.Concurrent.BlockingCollection")]
    [InlineData("System.Collections.ObjectModel.ReadOnlySet")]
    [InlineData("System.Collections.ObjectModel.Collection")]
    [InlineData("System.Collections.ObjectModel.ReadOnlyCollection")]
    public async Task ShouldSupportCollectionInjection(string collectionType, LanguageVersion languageVersion = LanguageVersion.CSharp9)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               struct Point
                               {
                                   int X, Y;
                                   
                                   public Point(int x, int y)
                                   {
                                       X = x;
                                       Y = y;
                                   }
                               } 
                           
                               interface IDependency {}
                           
                               class Dependency: IDependency, IComparable<Dependency>, IComparable
                               {        
                                   public Dependency(ReadOnlySpan<Point> points1, Span<Point> points2)
                                   {
                                       Console.WriteLine("Dependency created");
                                   }
                           
                                   public int CompareTo(Dependency other) => GetHashCode() - other.GetHashCode();
                                   
                                   public int CompareTo(object obj) => GetHashCode() - obj.GetHashCode();
                               }
                           
                               interface IService
                               {                    
                               }
                           
                               class Service: IService 
                               {
                                   public Service(###CollectionType###<IDependency> deps)
                                   { 
                                       Console.WriteLine("Service creating");
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>(1).To<Dependency>()
                                           .Bind<IDependency>(1).To<Dependency>()
                                           .Bind<IDependency>(2).To<Dependency>()
                                           .Bind<IDependency>(3).To<Dependency>()
                                           .Bind<Point>(1).To(_ => new Point(1, 2))
                                           .Bind<Point>(2).To(_ => new Point(2, 3))
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       try 
                                       {
                                           var composition = new Composition();
                                           var service = composition.Service;
                                       }
                                       catch(Exception ex)
                                       {
                                           Console.WriteLine(ex.Message);
                                       }
                                   }
                               }
                           }
                           """.Replace("###CollectionType###", collectionType).RunAsync(
            new Options
            {
                LanguageVersion = languageVersion,
                NullableContextOptions = NullableContextOptions.Disable
            });

        // Then
        result.Errors.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["Dependency created", "Dependency created", "Dependency created", "Service creating"], result);
    }

    [Theory]
    [InlineData("System.Collections.Generic.IList")]
    [InlineData("System.Collections.Immutable.ImmutableArray")]
    [InlineData("System.Collections.Generic.IReadOnlyList")]
    [InlineData("System.Collections.Generic.ISet")]
    [InlineData("System.Collections.Generic.HashSet")]
    [InlineData("System.Collections.Generic.SortedSet")]
    [InlineData("System.Collections.Generic.Queue")]
    [InlineData("System.Collections.Generic.Stack")]
    [InlineData("System.Collections.Immutable.IImmutableList")]
    [InlineData("System.Collections.Immutable.ImmutableList")]
    [InlineData("System.Collections.Immutable.IImmutableSet")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet")]
    [InlineData("System.Collections.Immutable.ImmutableSortedSet")]
    [InlineData("System.Collections.Immutable.IImmutableQueue")]
    [InlineData("System.Collections.Immutable.ImmutableQueue")]
    [InlineData("System.Collections.Immutable.IImmutableStack")]
    [InlineData("System.Collections.Immutable.ImmutableStack")]
    [InlineData("System.Span")]
    [InlineData("System.Span", LanguageVersion.CSharp8)]
    [InlineData("System.ReadOnlySpan")]
    [InlineData("System.Memory")]
    [InlineData("System.ReadOnlyMemory")]
    [InlineData("System.Collections.Concurrent.IProducerConsumerCollection")]
    [InlineData("System.Collections.Concurrent.ConcurrentBag")]
    [InlineData("System.Collections.Concurrent.ConcurrentQueue")]
    [InlineData("System.Collections.Concurrent.ConcurrentStack")]
    [InlineData("System.Collections.Concurrent.BlockingCollection")]
    [InlineData("System.Collections.ObjectModel.ReadOnlySet")]
    public async Task ShouldSupportCollectionInjectionWhenGeneric(string collectionType, LanguageVersion languageVersion = LanguageVersion.CSharp9)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               struct Point
                               {
                                   int X, Y;
                                   
                                   public Point(int x, int y)
                                   {
                                       X = x;
                                       Y = y;
                                   }
                               } 
                           
                               interface IDependency<T> {}
                           
                               class Dependency<T>: IDependency<T>, IComparable<Dependency<T>>, IComparable
                               {        
                                   public Dependency(ReadOnlySpan<Point> points1, Span<Point> points2)
                                   {
                                       Console.WriteLine("Dependency created");
                                   }
                           
                                   public int CompareTo(Dependency<T> other) => GetHashCode() - other.GetHashCode();
                                   
                                   public int CompareTo(object obj) => GetHashCode() - obj.GetHashCode();
                               }
                           
                               interface IService
                               {                    
                               }
                           
                               class Service: IService 
                               {
                                   public Service(###CollectionType###<IDependency<int>> deps)
                                   { 
                                       Console.WriteLine("Service creating");
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>(1).To<Dependency<TT>>()
                                           .Bind<IDependency<TT>>(2).To<Dependency<TT>>()
                                           .Bind<IDependency<TT>>(3).To<Dependency<TT>>()
                                           .Bind<Point>(1).To(_ => new Point(1, 2))
                                           .Bind<Point>(2).To(_ => new Point(2, 3))
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       try 
                                       {
                                           var composition = new Composition();
                                           var service = composition.Service;
                                       }
                                       catch(Exception ex)
                                       {
                                           Console.WriteLine(ex.Message);
                                       }
                                   }
                               }
                           }
                           """.Replace("###CollectionType###", collectionType).RunAsync(
            new Options
            {
                LanguageVersion = languageVersion,
                NullableContextOptions = NullableContextOptions.Disable
            });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency created", "Dependency created", "Dependency created", "Service creating"], result);
    }

    [Theory]
    [InlineData("System.Collections.Generic.IList")]
    [InlineData("System.Collections.Immutable.ImmutableArray")]
    [InlineData("System.Collections.Generic.IReadOnlyList")]
    [InlineData("System.Collections.Generic.ISet")]
    [InlineData("System.Collections.Generic.HashSet")]
    [InlineData("System.Collections.Generic.SortedSet")]
    [InlineData("System.Collections.Generic.Queue")]
    [InlineData("System.Collections.Generic.Stack")]
    [InlineData("System.Collections.Immutable.IImmutableList")]
    [InlineData("System.Collections.Immutable.ImmutableList")]
    [InlineData("System.Collections.Immutable.IImmutableSet")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet")]
    [InlineData("System.Collections.Immutable.ImmutableSortedSet")]
    [InlineData("System.Collections.Immutable.IImmutableQueue")]
    [InlineData("System.Collections.Immutable.ImmutableQueue")]
    [InlineData("System.Collections.Immutable.IImmutableStack")]
    [InlineData("System.Collections.Immutable.ImmutableStack")]
    [InlineData("System.Span")]
    [InlineData("System.Span", LanguageVersion.CSharp8)]
    [InlineData("System.ReadOnlySpan")]
    [InlineData("System.Memory")]
    [InlineData("System.ReadOnlyMemory")]
    [InlineData("System.Collections.Concurrent.IProducerConsumerCollection")]
    [InlineData("System.Collections.Concurrent.ConcurrentBag")]
    [InlineData("System.Collections.Concurrent.ConcurrentQueue")]
    [InlineData("System.Collections.Concurrent.ConcurrentStack")]
    [InlineData("System.Collections.Concurrent.BlockingCollection")]
    [InlineData("System.Collections.ObjectModel.Collection")]
    [InlineData("System.Collections.ObjectModel.ReadOnlyCollection")]
    public async Task ShouldSupportCollectionInjectionWhenItHasNoBindings(string collectionType, LanguageVersion languageVersion = LanguageVersion.CSharp9)
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
                               }
                           
                               class Service: IService 
                               {
                                   public Service(###CollectionType###<IDependency> deps)
                                   { 
                                       Console.WriteLine("Service creating");
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
                                       try 
                                       {
                                           var composition = new Composition();
                                           var service = composition.Service;
                                       }
                                       catch(Exception ex)
                                       {
                                           Console.WriteLine(ex.Message);
                                       }
                                   }
                               }
                           }
                           """.Replace("###CollectionType###", collectionType).RunAsync(
            new Options
            {
                LanguageVersion = languageVersion,
                NullableContextOptions = NullableContextOptions.Disable
            });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Service creating"], result);
    }

    [Theory]
    [InlineData("System.Collections.Generic.IAsyncEnumerable")]
    public async Task ShouldSupportAsyncCollectionInjection(string collectionType, LanguageVersion languageVersion = LanguageVersion.CSharp9)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               struct Point
                               {
                                   int X, Y;
                                   
                                   public Point(int x, int y)
                                   {
                                       X = x;
                                       Y = y;
                                   }
                               } 
                           
                               interface IDependency {}
                           
                               class Dependency: IDependency, IComparable<Dependency>, IComparable
                               {        
                                   public Dependency(Point[] points1, System.Collections.Generic.IList<Point> points2)
                                   {
                                       Console.WriteLine("Dependency created");
                                   }
                           
                                   public int CompareTo(Dependency other) => GetHashCode() - other.GetHashCode();
                                   
                                   public int CompareTo(object obj) => GetHashCode() - obj.GetHashCode();
                               }
                           
                               interface IService
                               {                    
                               }
                           
                               class Service: IService 
                               {
                                   public Service(###CollectionType###<IDependency> deps)
                                   { 
                                       foreach (var dependency in deps.ToBlockingEnumerable())
                                       {
                                       }
                           
                                       Console.WriteLine("Service creating");
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>(1).To<Dependency>()
                                           .Bind<IDependency>(2).To<Dependency>()
                                           .Bind<IDependency>(3).To<Dependency>()
                                           .Bind<Point>(1).To(_ => new Point(1, 2))
                                           .Bind<Point>(2).To(_ => new Point(2, 3))
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       try 
                                       {
                                           var composition = new Composition();
                                           var service = composition.Service;
                                       }
                                       catch(Exception ex)
                                       {
                                           Console.WriteLine(ex.Message);
                                       }
                                   }
                               }
                           }
                           """.Replace("###CollectionType###", collectionType).RunAsync(
            new Options
            {
                LanguageVersion = languageVersion,
                NullableContextOptions = NullableContextOptions.Disable
            });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency created", "Dependency created", "Dependency created", "Service creating"], result);
    }

    [Fact]
    public async Task ShouldSupportLazyInjection()
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
                           
                               interface IService {}
                           
                               class Service: IService 
                               {
                                   public Service(Lazy<IDependency> lazy)
                                   { 
                                       Console.WriteLine("Service creating");
                                       var dep = lazy.Value;
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

    [Fact]
    public async Task ShouldSupportFuncInjection()
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
                           
                               interface IService {}
                           
                               class Service: IService 
                               {
                                   public Service(Func<IDependency> func)
                                   { 
                                       Console.WriteLine("Service creating");
                                       var dep1 = func();
                                       var dep2 = func();
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
        result.StdOut.ShouldBe(["Service creating", "Dependency created", "Dependency created"], result);
    }

    [Theory]
    [InlineData("System.Collections.Generic.IEnumerable")]
    [InlineData("System.Collections.Generic.IReadOnlyCollection")]
    [InlineData("System.Collections.Generic.IReadOnlyList")]
    [InlineData("System.Collections.Generic.IList")]
    [InlineData("System.Collections.Generic.ICollection")]
    [InlineData("System.Collections.Immutable.ImmutableArray")]
    [InlineData("System.Collections.Immutable.IImmutableList")]
    [InlineData("System.Collections.Immutable.ImmutableList")]
    public async Task ShouldSupportCollectionOfLazyInjection(string collectionType)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using System.Collections.Immutable;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency1: IDependency 
                               {
                                   public Dependency1() => Console.WriteLine("Dependency created");
                               }

                               class Dependency2: IDependency 
                               {
                                   public Dependency2() => Console.WriteLine("Dependency created");
                               }
                           
                               interface IService {}
                           
                               class Service: IService 
                               {
                                   public Service(###CollectionType###<Lazy<IDependency>> deps)
                                   { 
                                       Console.WriteLine("Service creating");
                                       foreach(var dep in deps)
                                       {
                                           var unused = dep.Value;
                                       }
                                   }
                               }
                       
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency1>()
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
        result.StdOut.ShouldBe(["Service creating", "Dependency created"], result);
    }

    [Theory]
    [InlineData("System.Collections.Generic.IEnumerable")]
    [InlineData("System.Collections.Generic.IReadOnlyCollection")]
    [InlineData("System.Collections.Generic.IReadOnlyList")]
    [InlineData("System.Collections.Generic.IList")]
    [InlineData("System.Collections.Generic.ICollection")]
    [InlineData("System.Collections.Immutable.ImmutableArray")]
    [InlineData("System.Collections.Immutable.IImmutableList")]
    [InlineData("System.Collections.Immutable.ImmutableList")]
    public async Task ShouldSupportCollectionOfFuncInjection(string collectionType)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using System.Collections.Immutable;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency1: IDependency 
                               {
                                   public Dependency1() => Console.WriteLine("Dependency created");
                               }

                               class Dependency2: IDependency 
                               {
                                   public Dependency2() => Console.WriteLine("Dependency created");
                               }
                           
                               interface IService {}
                           
                               class Service: IService 
                               {
                                   public Service(###CollectionType###<Func<IDependency>> deps)
                                   { 
                                       Console.WriteLine("Service creating");
                                       foreach(var dep in deps)
                                       {
                                           var unused = dep();
                                       }
                                   }
                               }
                       
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency1>()
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
        result.StdOut.ShouldBe(["Service creating", "Dependency created"], result);
    }

    [Fact]
    public async Task ShouldSupportThreadLocalInjection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading;
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
                                   public Service(ThreadLocal<IDependency> threadLocal)
                                   { 
                                       Console.WriteLine("Service creating");
                                       var dep = threadLocal.Value;
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

    [Fact]
    public async Task ShouldSupportTaskInjection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
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
                                   public Service(Task<IDependency> task)
                                   { 
                                       var dep = task.Result;
                                       Console.WriteLine("Service creating");
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
        result.StdOut.ShouldBe(["Dependency created", "Service creating"], result);
    }

    [Fact]
    public async Task ShouldSupportFuncWithArgumentsInjection()
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
                                   public Dependency(string name, int id) => Console.WriteLine($"Dependency {name} {id} created");
                               }
                           
                               interface IService {}
                           
                               class Service: IService 
                               {
                                   public Service(Func<string, int, IDependency> factory)
                                   { 
                                       Console.WriteLine("Service creating");
                                       var dep1 = factory("A", 1);
                                       var dep2 = factory("B", 2);
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
        result.StdOut.ShouldBe(["Service creating", "Dependency A 1 created", "Dependency B 2 created"], result);
    }

    [Fact]
    public async Task ShouldSupportActionInjectionWhenBlockAndArgIsNotUsed()
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

                               class Service
                               {
                                   public Service(Action action) 
                                   {
                                       action();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<Action>().As(Lifetime.PerResolve).To(ctx => { return new Action(() => { ctx.Inject<IDependency>(out _); }); } )
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
        result.StdOut.ShouldBe(["Dependency created"], result);
    }

    [Fact]
    public async Task ShouldSupportActionInjectionWhenBlock()
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

                               class Service
                               {
                                   public Service(Action action) 
                                   {
                                       action();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<Action>().As(Lifetime.PerResolve).To(ctx => { return new Action(() => { ctx.Inject<IDependency>(out var val); }); } )
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
        result.StdOut.ShouldBe(["Dependency created"], result);
    }

    [Fact]
    public async Task ShouldSupportActionInjectionWhenExpression()
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

                               class Service
                               {
                                   public Service(Action action) 
                                   {
                                       action();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<Action>().As(Lifetime.PerResolve).To(ctx => new Action(() => ctx.Inject<IDependency>(out var abc)))
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
        result.StdOut.ShouldBe(["Dependency created"], result);
    }

    [Fact]
    public async Task ShouldSupportValueTaskInjection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency: IDependency {}

                               class Service
                               {
                                   public Service(ValueTask<IDependency> depTask) 
                                   {
                                       Console.WriteLine(depTask.IsCompleted);
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
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportComplexCompositionWhenCyclesViaEnumerable()
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
                               public interface IProviderA { string GetData(); }
                               public interface IProviderB { int GetNumber(); }
                               public interface IServiceA {}
                               public interface IServiceB {}
                               public interface IServiceC {}
                               public interface IProviderA1 {}
                               public interface IProviderA2 {}
                               public interface IProviderB1 {}
                               public interface IProviderB2 {}

                               public class ServiceA : IServiceA
                               {
                                   public ServiceA(IEnumerable<IProviderA> providers)
                                   {
                                       if (!providers.Any()) throw new Exception("ServiceA: providers are empty");
                                   }
                               }

                               public class ServiceB : IServiceB
                               {
                                   public ServiceB(IEnumerable<IProviderB> providers)
                                   {
                                       if (!providers.Any()) throw new Exception("ServiceB: providers are empty");
                                   }
                               }

                               public class ServiceC : IServiceC
                               {
                                   public ServiceC(IServiceB serviceB)
                                   {
                                       if (serviceB == null) throw new Exception("ServiceC: serviceB is null");
                                   }
                               }

                               public class Manager
                               {
                                   public Manager(IServiceC serviceC, IProviderA1 providerA1)
                                   {
                                       if (serviceC == null) throw new Exception("Manager: serviceC is null");
                                       if (providerA1 == null) throw new Exception("Manager: providerA1 is null");
                                   }
                               }

                               public class ProviderA1 : IProviderA, IProviderA1
                               {
                                   public string GetData() => "ProviderA1 Data";
                               }

                               public class ProviderA2 : IProviderA, IProviderA2
                               {
                                   public string GetData() => "ProviderA2 Data";
                               }

                               public class ProviderB1 : IProviderB, IProviderB1
                               {
                                   public ProviderB1(IServiceC serviceC)
                                   {
                                       if (serviceC == null) throw new Exception("ProviderB1: serviceC is null");
                                   }
                                   public int GetNumber() => 1;
                               }

                               public class ProviderB2 : IProviderB, IProviderB2
                               {
                                   public int GetNumber() => 2;
                               }

                               public class Root
                               {
                                   public Root(Manager manager)
                                   {
                                       if (manager == null) throw new Exception("Root: manager is null");
                                   }

                                   public void Run() => Console.WriteLine("Composition Root is running!");
                               }

                               internal partial class CompositionCore
                               {
                                   void Setup() => DI.Setup(nameof(CompositionCore), CompositionKind.Internal)
                                       .DefaultLifetime(Lifetime.Singleton)
                                       .Bind<IServiceA>().To<ServiceA>()
                                       .Bind<IServiceB>().To<ServiceB>()
                                       .Bind<IServiceC>().To<ServiceC>();
                               }

                               internal partial class CompositionImplementation
                               {
                                   void Setup() => DI.Setup("Composition")
                                       .DependsOn(nameof(CompositionCore))
                                       .DefaultLifetime(Lifetime.Singleton)
                                       .Bind<IProviderA>(Tag.Unique).Bind<IProviderA1>().To<ProviderA1>()
                                       .Bind<IProviderB>(Tag.Unique).Bind<IProviderB1>().To<ProviderB1>()
                                       .Bind<IProviderB>(Tag.Unique).Bind<IProviderB2>().To<ProviderB2>()
                                       .Bind<Manager>().To<Manager>()
                                       .Root<Root>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Composition Root is running!"], result);
    }

    [Fact]
    public async Task ShouldSupportMoreComplexCompositionWhenCyclesViaEnumerable()
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
                               public interface IProviderA { string GetData(); }
                               public interface IProviderB { int GetNumber(); }
                               public interface IServiceA {}
                               public interface IServiceB {}
                               public interface IServiceC {}
                               public interface IProviderA1 {}
                               public interface IProviderA2 {}
                               public interface IProviderB1 {}
                               public interface IProviderB2 {}

                               public class ServiceA : IServiceA
                               {
                                   public ServiceA(IEnumerable<IProviderA> providers)
                                   {
                                       if (!providers.Any()) throw new Exception("ServiceA: providers are empty");
                                   }
                               }

                               public class ServiceB : IServiceB
                               {
                                   public ServiceB(IEnumerable<IProviderB> providers)
                                   {
                                       if (!providers.Any()) throw new Exception("ServiceB: providers are empty");
                                   }
                               }

                               public class ServiceC : IServiceC
                               {
                                   public ServiceC(ServiceB serviceB, ServiceA serviceA)
                                   {
                                       if (serviceB == null) throw new Exception("ServiceC: serviceB is null");
                                       if (serviceA == null) throw new Exception("ServiceC: serviceA is null");
                                   }
                               }

                               public class Manager
                               {
                                   public Manager(IServiceC serviceC, IProviderA1 providerA1, IProviderB1 providerB1)
                                   {
                                       if (serviceC == null) throw new Exception("Manager: serviceC is null");
                                       if (providerA1 == null) throw new Exception("Manager: providerA1 is null");
                                       if (providerB1 == null) throw new Exception("Manager: providerB1 is null");
                                   }
                               }

                               public class ProviderA1 : IProviderA, IProviderA1
                               {
                                   public string GetData() => "ProviderA1 Data";
                               }

                               public class ProviderA2 : IProviderA, IProviderA2
                               {
                                   public string GetData() => "ProviderA2 Data";
                               }

                               public class ProviderB1 : IProviderB, IProviderB1
                               {
                                   public ProviderB1(IServiceC serviceC)
                                   {
                                       if (serviceC == null) throw new Exception("ProviderB1: serviceC is null");
                                   }
                                   public int GetNumber() => 1;
                               }

                               public class ProviderB2 : IProviderB, IProviderB2
                               {
                                   public int GetNumber() => 2;
                               }

                               public class Root
                               {
                                   public Root(Manager manager)
                                   {
                                       if (manager == null) throw new Exception("Root: manager is null");
                                   }

                                   public void Run() => Console.WriteLine("Composition Root is running!");
                               }

                               internal partial class CompositionCore
                               {
                                   void Setup() => DI.Setup(nameof(CompositionCore), CompositionKind.Internal)
                                       .DefaultLifetime(Lifetime.Singleton)
                                       .Bind<IServiceA>().To<ServiceA>()
                                       .Bind<IServiceB>().To<ServiceB>()
                                       .Bind<IServiceC>().To<ServiceC>();
                               }

                               internal partial class CompositionImplementation
                               {
                                   void Setup() => DI.Setup("Composition")
                                       .DependsOn(nameof(CompositionCore))
                                       .DefaultLifetime(Lifetime.Singleton)
                                       .Bind<IProviderA>(Tag.Unique).Bind<IProviderA1>().To<ProviderA1>()
                                       .Bind<IProviderB>(Tag.Unique).Bind<IProviderB1>().To<ProviderB1>()
                                       .Bind<IProviderB>(Tag.Unique).Bind<IProviderB2>().To<ProviderB2>()
                                       .Bind<Manager>().To<Manager>()
                                       .Root<Root>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Composition Root is running!"], result);
    }

    [Fact]
    public async Task ShouldSupportComplexCircularDependencyViaEnumerableAndFunc()
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
                               public interface IA { }
                               public interface IB { }
                               public interface IC { }

                               public class A : IA
                               {
                                   public A(IEnumerable<IB> b) { }
                               }

                               public class B : IB
                               {
                                   public B(Func<IC> cFactory) { }
                               }

                               public class C : IC
                               {
                                   public C(IA a) { }
                               }

                               internal partial class Composition
                               {
                                   void Setup() => DI.Setup(nameof(Composition))
                                       .DefaultLifetime(Lifetime.Singleton)
                                       .Bind<IA>().To<A>()
                                       .Bind<IB>().To<B>()
                                       .Bind<IC>().To<C>()
                                       .Root<IA>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine("Composition Root is running!");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Composition Root is running!"], result);
    }

    [Fact]
    public async Task ShouldSupportCircularDependencyViaLazy()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public interface IA { }
                               public interface IB { }

                               public class A : IA
                               {
                                   public A(Lazy<IB> b) { }
                               }

                               public class B : IB
                               {
                                   public B(IA a) { }
                               }

                               internal partial class Composition
                               {
                                   void Setup() => DI.Setup(nameof(Composition))
                                       .Bind<IA>().To<A>()
                                       .Bind<IB>().To<B>()
                                       .Root<IA>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine("Composition Root is running!");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Composition Root is running!"], result);
    }

    [Fact]
    public async Task ShouldSupportCircularDependencyViaFunc()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public interface IA { }
                               public interface IB { }

                               public class A : IA
                               {
                                   public A(Func<IB> bFactory) { }
                               }

                               public class B : IB
                               {
                                   public B(IA a) { }
                               }

                               internal partial class Composition
                               {
                                   void Setup() => DI.Setup(nameof(Composition))
                                       .Bind<IA>().To<A>()
                                       .Bind<IB>().To<B>()
                                       .Root<IA>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine("Composition Root is running!");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Composition Root is running!"], result);
    }

    [Fact]
    public async Task ShouldSupportDictionaryInjection()
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

                               class DependencyA : IDependency
                               {
                                   public DependencyA()
                                   {
                                       Console.WriteLine("DependencyA created");
                                   }
                               }

                               class DependencyB : IDependency
                               {
                                   public DependencyB()
                                   {
                                       Console.WriteLine("DependencyB created");
                                   }
                               }
                               
                               class DependencyC : IDependency
                               {
                                   public DependencyC()
                                   {
                                       Console.WriteLine("DependencyC created");
                                   }
                               }

                               class Service
                               {
                                   public Service(IDictionary<string, IDependency> dependencies)
                                   {
                                       Console.WriteLine("Service created with dependencies count: " + dependencies.Count.ToString());
                                   }
                               }

                               internal partial class Composition
                               {
                                   void Setup() => DI.Setup(nameof(Composition))
                                       .Bind(Tag.Unique).To((DependencyA dep) => new KeyValuePair<string, IDependency>("A", dep))
                                       .Bind(Tag.Unique).To((DependencyB dep) => new KeyValuePair<string, IDependency>("B", dep))
                                       .Bind(Tag.Unique).To((DependencyC dep) => new KeyValuePair<string, IDependency>("C", dep))
                                       .Root<Service>("MyService");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       try
                                       {
                                           var composition = new Composition();
                                           var myService = composition.MyService;
                                       }
                                       catch (Exception ex)
                                       {
                                           Console.WriteLine(ex.Message);
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["DependencyA created", "DependencyB created", "DependencyC created", "Service created with dependencies count: 3"], result);
    }
}
