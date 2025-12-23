namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the support for IDisposable and IAsyncDisposable in the composition.
/// </summary>
public class DisposableTests
{

    [Fact]
    public async Task ShouldSupportDisposableWhenNotThreadSafe()
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
                           
                               class Dependency: IDependency, IDisposable, IAsyncDisposable
                               {
                                   public void Dispose()
                                   {
                                       Console.WriteLine("Dispose");
                                   }
                                   
                                   public ValueTask DisposeAsync()
                                   {
                                       Console.WriteLine("DisposeAsync");
                                       return ValueTask.CompletedTask;
                                   }
                               }
                           
                              interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep)
                                   {
                                       Dep = dep;
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Bind().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using var composition = new Composition();
                                       var service = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonComplexWithAsyncDisposableAndDisposableWhenDispose()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Threading.Tasks;

                           namespace Sample
                           {
                               interface IDependency1 {}
                           
                               class Dependency1: IDependency1 {}
                           
                               interface IDependency2
                               {
                                   IDependency1 Dep1 { get; }
                               }
                           
                               class Dependency2: IDependency2, IDisposable
                               {
                                   public Dependency2(IDependency1 dep1)
                                   {
                                       Dep1 = dep1;
                                       Console.WriteLine("Dependency2");
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public void Dispose()
                                   {
                                       Console.WriteLine("Dispose2");
                                   }
                               }
                           
                               interface IDependency3
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                               }
                           
                               class Dependency3: IDisposable, IAsyncDisposable, IDependency3
                               {
                                   public Dependency3(IDependency1 dep1, IDependency2 dep2)
                                   { 
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Console.WriteLine("Dependency3");
                                   }       
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                                   
                                   public void Dispose()
                                   {
                                       Console.WriteLine("Dispose3");
                                   }
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       Console.WriteLine("DisposeAsync3");
                                       return ValueTask.CompletedTask;
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                           
                                   IDependency3 Dep3 { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency3 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                           
                                   public IDependency3 Dep3 { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency1>().To<Dependency1>()
                                           .Bind<IDependency2>().As(Lifetime.Singleton).To<Dependency2>()
                                           .Bind<IDependency3>().As(Lifetime.Singleton).To<Dependency3>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using var composition = new Composition();
                                       var service1 = composition.Service;
                                       var service2 = composition.Service;
                                       
                                       using var composition2 = new Composition();
                                       var service3 = composition2.Service;
                                       var service4 = composition2.Service;
                                       var valueTask = composition2.DisposeAsync();
                                       if (valueTask.IsCompleted)
                                       {
                                           valueTask.AsTask().Wait();
                                       }    
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency2", "Dependency3", "Dependency2", "Dependency3", "DisposeAsync3", "Dispose2", "Dispose3", "Dispose2"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonComplexWithAsyncDisposableOnlyWhenDispose()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Threading.Tasks;

                           namespace Sample
                           {
                               interface IDependency1 {}
                           
                               class Dependency1: IDependency1 {}
                           
                               interface IDependency2
                               {
                                   IDependency1 Dep1 { get; }
                               }
                           
                               class Dependency2: IDependency2, IAsyncDisposable
                               {
                                   public Dependency2(IDependency1 dep1)
                                   {
                                       Dep1 = dep1;
                                       Console.WriteLine("Dependency2");
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       Console.WriteLine("DisposeAsync2");
                                       return ValueTask.CompletedTask;
                                   }
                               }
                           
                               interface IDependency3
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                               }
                           
                               class Dependency3: IAsyncDisposable, IDependency3
                               {
                                   public Dependency3(IDependency1 dep1, IDependency2 dep2)
                                   { 
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Console.WriteLine("Dependency3");
                                   }       
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                                   
                                   public ValueTask DisposeAsync()
                                   {
                                       Console.WriteLine("DisposeAsync3");
                                       return ValueTask.CompletedTask;
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                           
                                   IDependency3 Dep3 { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency3 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                           
                                   public IDependency3 Dep3 { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency1>().To<Dependency1>()
                                           .Bind<IDependency2>().As(Lifetime.Singleton).To<Dependency2>()
                                           .Bind<IDependency3>().As(Lifetime.Singleton).To<Dependency3>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using var composition = new Composition();
                                       var service1 = composition.Service;
                                       var service2 = composition.Service;
                                       
                                       using var composition2 = new Composition();
                                       var service3 = composition2.Service;
                                       var service4 = composition2.Service;
                                       var valueTask = composition2.DisposeAsync();
                                       if (valueTask.IsCompleted)
                                       {
                                           valueTask.AsTask().Wait();
                                       }    
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency2", "Dependency3", "Dependency2", "Dependency3", "DisposeAsync3", "DisposeAsync2", "DisposeAsync3", "DisposeAsync2"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonComplexWithAsyncDisposableWhenDispose()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Threading.Tasks;

                           namespace Sample
                           {
                               interface IDependency1 {}
                           
                               class Dependency1: IDependency1 {}
                           
                               interface IDependency2
                               {
                                   IDependency1 Dep1 { get; }
                               }
                           
                               class Dependency2: IDependency2, IDisposable
                               {
                                   public Dependency2(IDependency1 dep1)
                                   {
                                       Dep1 = dep1;
                                       Console.WriteLine("Dependency2");
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public void Dispose()
                                   {
                                       Console.WriteLine("Dispose2");
                                   }
                               }
                           
                               interface IDependency3
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                               }
                           
                               class Dependency3: IAsyncDisposable, IDependency3
                               {
                                   public Dependency3(IDependency1 dep1, IDependency2 dep2)
                                   { 
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Console.WriteLine("Dependency3");
                                   }       
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                                   
                                   public ValueTask DisposeAsync()
                                   {
                                       Console.WriteLine("DisposeAsync3");
                                       return ValueTask.CompletedTask;
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                           
                                   IDependency3 Dep3 { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency3 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                           
                                   public IDependency3 Dep3 { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency1>().To<Dependency1>()
                                           .Bind<IDependency2>().As(Lifetime.Singleton).To<Dependency2>()
                                           .Bind<IDependency3>().As(Lifetime.Singleton).To<Dependency3>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using var composition = new Composition();
                                       var service1 = composition.Service;
                                       var service2 = composition.Service;
                                       
                                       using var composition2 = new Composition();
                                       var service3 = composition2.Service;
                                       var service4 = composition2.Service;
                                       var valueTask = composition2.DisposeAsync();
                                       if (valueTask.IsCompleted)
                                       {
                                           valueTask.AsTask().Wait();
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency2", "Dependency3", "Dependency2", "Dependency3", "DisposeAsync3", "Dispose2", "DisposeAsync3", "Dispose2"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonComplexWithDisposable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency1 {}
                           
                               class Dependency1: IDependency1 {}
                           
                               interface IDependency2
                               {
                                   IDependency1 Dep1 { get; }
                               }
                           
                               class Dependency2: IDependency2, IDisposable
                               {
                                   public Dependency2(IDependency1 dep1)
                                   {
                                       Dep1 = dep1;
                                       Console.WriteLine("Dependency2");
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public void Dispose()
                                   {
                                       Console.WriteLine("Dispose2");
                                   }
                               }
                           
                               interface IDependency3
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                               }
                           
                               class Dependency3: IDisposable, IDependency3
                               {
                                   public Dependency3(IDependency1 dep1, IDependency2 dep2)
                                   { 
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Console.WriteLine("Dependency3");
                                   }       
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                           
                                   public void Dispose()
                                   {
                                       Console.WriteLine("Dispose3");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                           
                                   IDependency3 Dep3 { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency3 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                           
                                   public IDependency3 Dep3 { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency1>().To<Dependency1>()
                                           .Bind<IDependency2>().As(Lifetime.Singleton).To<Dependency2>()
                                           .Bind<IDependency3>().As(Lifetime.Singleton).To<Dependency3>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using var composition = new Composition();
                                       var service1 = composition.Service;
                                       var service2 = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency2", "Dependency3", "Dispose3", "Dispose2"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonWithDisposable()
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
                           
                               class Dependency: IDependency, IDisposable, IAsyncDisposable
                               {
                                   public void Dispose()
                                   {
                                       Console.WriteLine("Dispose");
                                   }
                                   
                                   public ValueTask DisposeAsync()
                                   {
                                       Console.WriteLine("DisposeAsync");
                                       return ValueTask.CompletedTask;
                                   }
                               }
                           
                              interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep)
                                   {
                                       Dep = dep;
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using var composition = new Composition();
                                       var service = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose"], result);
    }

    [Fact]
    public async Task ShouldSupportDisposableFromFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency: IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose");
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Dependency>().As(Lifetime.Singleton).To(_ => new Dependency())
                                           .Root<Dependency>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var root = composition.Root;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithDisposable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency: IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose");
                               }

                               class Service
                               {
                                   public Dependency Dep1 { get; }
                                   public Dependency Dep2 { get; }

                                   public Service(Dependency dep1, Dependency dep2)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Dependency>().As(Lifetime.PerResolve).To<Dependency>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(object.ReferenceEquals(root.Dep1, root.Dep2));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportAsyncDisposeWhenOnlyDisposable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency: IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose");
                               }

                               class AsyncDependency: IAsyncDisposable
                               {
                                   public ValueTask DisposeAsync() => ValueTask.CompletedTask;
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Dependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<AsyncDependency>().As(Lifetime.Singleton).To<AsyncDependency>()
                                           .Root<Dependency>("Root")
                                           .Root<AsyncDependency>("AsyncRoot");
                                   }
                               }

                               public class Program
                               {
                                   public static async Task Main()
                                   {
                                       await using (var composition = new Composition())
                                       {
                                           var root = composition.Root;
                                           var asyncRoot = composition.AsyncRoot;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose"], result);
    }

    [Fact]
    public async Task ShouldSupportSharedDisposableSingletonBetweenRoots()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency: IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose");
                               }

                               class Service1
                               {
                                   public Service1(Dependency dep) {}
                               }

                               class Service2
                               {
                                   public Service2(Dependency dep) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Dependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Root<Service1>("Root1")
                                           .Root<Service2>("Root2");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var s1 = composition.Root1;
                                           var s2 = composition.Root2;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose"], result);
    }

    [Fact]
    public async Task ShouldNotDisposeTransient()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency: IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Dependency>().To<Dependency>()
                                           .Root<Dependency>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([], result);
    }

    [Fact]
    public async Task ShouldSupportDisposableInArray()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep: IDisposable {}
                               class Dep1: IDep { public void Dispose() => Console.WriteLine("Dispose 1"); }
                               class Dep2: IDep { public void Dispose() => Console.WriteLine("Dispose 2"); }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.Singleton).To<Dep1>()
                                           .Bind<IDep>(2).As(Lifetime.Singleton).To<Dep2>()
                                           .Root<IDep[]>("Roots");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var roots = composition.Roots;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose 2", "Dispose 1"], result);
    }

    [Fact]
    public async Task ShouldSupportScopedWithDisposable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency: IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Dependency>().As(Lifetime.Scoped).To<Dependency>()
                                           .Root<Dependency>("Dep");
                                   }
                               }
                               
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var dep = composition.Dep;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose"], result);
    }

    [Fact]
    public async Task ShouldSupportIDisposableAndIAsyncDisposableInSameClassBoundSeparately()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface I1 {}
                               interface I2 {}
                               class Dependency: I1, I2, IDisposable, IAsyncDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose");
                                   public ValueTask DisposeAsync()
                                   {
                                       Console.WriteLine("DisposeAsync");
                                       return ValueTask.CompletedTask;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<I1>().Bind<I2>().As(Lifetime.Singleton).To<Dependency>()
                                           .Root<I1>("Root1")
                                           .Root<I2>("Root2");
                                   }
                               }

                               public class Program
                               {
                                   public static async Task Main()
                                   {
                                       var composition = new Composition();
                                       var r1 = composition.Root1;
                                       var r2 = composition.Root2;
                                       await composition.DisposeAsync();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        // IAsyncDisposable should take precedence if DisposeAsync is called
        result.StdOut.ShouldBe(["DisposeAsync"], result);
    }

    [Fact]
    public async Task ShouldSupportDisposableInList()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep: IDisposable {}
                               class Dep1: IDep { public void Dispose() => Console.WriteLine("Dispose 1"); }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.Singleton).To<Dep1>()
                                           .Root<IList<IDep>>("Roots");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var roots = composition.Roots;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose 1"], result);
    }

    [Fact]
    public async Task ShouldSupportDisposableInHashSet()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep: IDisposable {}
                               class Dep1: IDep { public void Dispose() => Console.WriteLine("Dispose 1"); }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.Singleton).To<Dep1>()
                                           .Root<ISet<IDep>>("Roots");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var roots = composition.Roots;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose 1"], result);
    }


    [Fact]
    public async Task ShouldSupportDisposableInReadOnlyCollection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep: IDisposable {}
                               class Dep1: IDep { public void Dispose() => Console.WriteLine("Dispose 1"); }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.Singleton).To<Dep1>()
                                           .Root<IReadOnlyCollection<IDep>>("Roots");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var roots = composition.Roots;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose 1"], result);
    }

    [Fact]
    public async Task ShouldSupportDisposableInCollection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep: IDisposable {}
                               class Dep1: IDep { public void Dispose() => Console.WriteLine("Dispose 1"); }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.Singleton).To<Dep1>()
                                           .Root<ICollection<IDep>>("Roots");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var roots = composition.Roots;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose 1"], result);
    }

    [Fact]
    public async Task ShouldSupportDisposableInReadOnlyList()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep: IDisposable {}
                               class Dep1: IDep { public void Dispose() => Console.WriteLine("Dispose 1"); }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.Singleton).To<Dep1>()
                                           .Root<IReadOnlyList<IDep>>("Roots");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var roots = composition.Roots;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose 1"], result);
    }

    [Fact]
    public async Task ShouldSupportDisposableGenericType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency<T>: IDisposable
                               {
                                   public void Dispose() => Console.WriteLine($"Dispose {typeof(T).Name}");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Dependency<TT>>().As(Lifetime.Singleton).To<Dependency<TT>>()
                                           .Root<Dependency<int>>("RootInt")
                                           .Root<Dependency<string>>("RootString");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var r1 = composition.RootInt;
                                           var r2 = composition.RootString;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose String", "Dispose Int32"], result);
    }

    [Fact]
    public async Task ShouldSupportMultipleDisposeCalls()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency: IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Dependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Root<Dependency>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       composition.Dispose();
                                       composition.Dispose();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose"], result);
    }

    [Fact]
    public async Task ShouldSupportDisposableWithArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency: IDisposable
                               {
                                   public Dependency(string name) {}
                                   public void Dispose() => Console.WriteLine("Dispose");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.Resolve, "Off")
                                           .Bind<Dependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .RootArg<string>("name")
                                           .Root<Dependency>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root("test");
                                       // Only singletons are disposed
                                       composition.Dispose();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose"], result);
    }

    [Fact]
    public async Task ShouldSupportDisposableWithFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency: IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Dependency>().As(Lifetime.Singleton).To(_ => new Dependency())
                                           .Root<Dependency>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var root = composition.Root;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose"], result);
    }
}