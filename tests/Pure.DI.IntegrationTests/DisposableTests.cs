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
}