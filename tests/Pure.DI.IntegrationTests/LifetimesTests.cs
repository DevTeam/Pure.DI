namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the support for various lifetimes (Transient, Singleton, Scoped, etc.).
/// </summary>
public class LifetimesTests
{

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("PerResolve")]
    public async Task ShouldSupportSingletonScopedPerResolveInEnumerable(string lifetime)
    {
        // Given

        // When
        var result = await """
                               using System;
                               using Pure.DI;
                               using System.Collections.Generic;

                               namespace Sample
                               {
                               interface IService {};

                               class StagingService : IService
                               {
                                   public StagingService() => Console.WriteLine(GetType());
                               }

                               class RegistrationService : IService
                               {
                                   public RegistrationService() => Console.WriteLine(GetType());
                               }

                               class ServiceHost
                               {
                                   public ServiceHost(IEnumerable<IService> services)
                                   {
                                       foreach (var service in services)
                                       {
                                       }
                                       
                                       foreach (var service in services)
                                       {
                                       }
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind(1).As(Lifetime.#Lifetime).To<StagingService>()
                                           .Bind(2).As(Lifetime.PerResolve).To<RegistrationService>()
                                           .Root<ServiceHost>("Host");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var Host = composition.Host;
                                   }
                               }
                           }
                           """.Replace("#Lifetime", lifetime).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.StagingService", "Sample.RegistrationService"], result);
    }

    [Fact]
    public async Task ShouldAssumeTransientWhenNotSpecified()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDefaultLifetimeForInstance()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime<Service>(Lifetime.Singleton)
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDefaultLifetimeForInstanceWhenAutoBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime<Service>(Lifetime.PerBlock)
                                           .DefaultLifetime<Service>(Lifetime.Singleton)
                                           .Root<Service>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDefaultLifetimeForInstanceWhenTag()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime<Service>(Lifetime.Singleton, "abc", "xyz")
                                           .Bind("xyz").To<Service>()
                                           .Root<IService>("Service", "xyz");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDefaultLifetimeForInstanceWhenTagIsNotMatch()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime<Service>(Lifetime.Singleton, "abc", "xyz")
                                           .Bind("asd").To<Service>()
                                           .Root<IService>("Service", "asd");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldDefaultLifetimeForInstanceWhenTagsArray()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime<Service>(Lifetime.Singleton, ["abc", "xyz"])
                                           .Bind("xyz").To<Service>()
                                           .Root<IService>("Service", "xyz");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }
#endif

    [Fact]
    public async Task ShouldDefaultLifetimeForInterface()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime<IService>(Lifetime.Singleton)
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseDefaultLifetimeWhenTypeDoesNotMatch()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime<string>(Lifetime.Singleton)
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseDefaultLifetimeWhenTypeDoesNotMatchButHasDefaultLifetime()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime<string>(Lifetime.Singleton)
                                           .DefaultLifetime(Lifetime.Singleton)
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldResetDefaultLifetimeForEachSetup()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupBaseComposition()
                                   {
                                       DI.Setup("BaseComposition", CompositionKind.Internal)
                                           .DefaultLifetime(Lifetime.Singleton);
                                   }

                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition").DependsOn("BaseComposition")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlock()
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

                               class Dependency2: IDependency2
                               {
                                   public Dependency2(IDependency1 dep1) => Dep1 = dep1;

                                   public IDependency1 Dep1 { get; }
                               }

                               interface IService
                               {
                                   IDependency1 Dep1 { get; }

                                   IDependency2 Dep2 { get; }
                                   
                                   IDependency1 Dep3 { get; }
                               }

                               class Service: IService
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency1 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }

                                   public IDependency1 Dep1 { get; }

                                   public IDependency2 Dep2 { get; }
                                   
                                   public IDependency1 Dep3 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency1>().As(Lifetime.PerBlock).To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
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
                                       Console.WriteLine(service.Dep1 == service.Dep2.Dep1);
                                       Console.WriteLine(service.Dep1 == service.Dep3);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockBinding()
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

                               class Dependency2: IDependency2
                               {
                                   public Dependency2(IDependency1 dep1) => Dep1 = dep1;

                                   public IDependency1 Dep1 { get; }
                               }

                               interface IService
                               {
                                   IDependency1 Dep1 { get; }

                                   IDependency2 Dep2 { get; }
                                   
                                   IDependency1 Dep3 { get; }
                               }

                               class Service: IService
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency1 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }

                                   public IDependency1 Dep1 { get; }

                                   public IDependency2 Dep2 { get; }
                                   
                                   public IDependency1 Dep3 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .PerBlock<Dependency1>()
                                           .Transient<Dependency2, Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep1 == service.Dep2.Dep1);
                                       Console.WriteLine(service.Dep1 == service.Dep3);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldReproduceMissingPerBlockOwnedInVirtualRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IAppViewModel {}
                               interface IClockViewModel {}
                               interface IClockModel {}
                               interface ITicks : IDisposable {}
                               interface ILog<T>
                               {
                                   void Info(string message);
                               }

                               sealed class ClockModel: IClockModel {}

                               sealed class DebugLog<T>: ILog<T>
                               {
                                   public DebugLog(IClockModel clockModel) {}

                                   public void Info(string message) {}
                               }

                               sealed class Ticks: ITicks
                               {
                                   public void Dispose() {}
                               }

                               sealed class ClockViewModel: IAppViewModel, IClockViewModel, IDisposable
                               {
                                   public ClockViewModel(ILog<ClockViewModel> log, IClockModel clockModel, ITicks ticks)
                                   {
                                   }

                                   public void Dispose() {}
                               }

                               sealed class DesignTimeAppViewModel: IAppViewModel {}
                               sealed class DesignTimeClockViewModel: IClockViewModel {}

                               sealed class MainWindow {}

                               readonly record struct Root(
                                   IOwned Owned,
                                   Func<MainWindow> CreateMainWindow,
                                   IAppViewModel App,
                                   IClockViewModel Clock);

                               partial class Composition
                               {
                                   void Setup() => DI.Setup()
                                       .Root<Root>(nameof(Root), kind: RootKinds.Virtual)
                                       .Bind().As(Lifetime.Singleton).To<Root>()
                                       .Bind().As(Lifetime.Singleton).To<ClockViewModel>()
                                       .Bind().To<ClockModel>()
                                       .Bind().As(Lifetime.Singleton).To<Ticks>()
                                       .Bind().To<DebugLog<TT>>();
                               }

                               partial class DesignTimeComposition: Composition
                               {
                                   [System.Diagnostics.Conditional("DI")]
                                   private void Setup() => DI.Setup()
                                       .Hint(Hint.Resolve, "Off")
                                       .Root<Root>(nameof(Root), kind: RootKinds.Override)
                                       .Bind().To<DesignTimeAppViewModel>()
                                       .Bind().To<DesignTimeClockViewModel>();
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       root.Owned.Dispose();
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWhenFunc()
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
                                   public Service(Func<IDependency> dep, Func<IDependency> dep2)
                                   {
                                       Console.WriteLine(dep == dep2);
                                       Dep = dep();
                                   }

                                   public IDependency Dep { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
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
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWhenSeveral()
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

                               class Dependency2: IDependency2
                               {
                                   public Dependency2(IDependency1 dep1) => Dep1 = dep1;

                                   public IDependency1 Dep1 { get; }
                               }

                               interface IService
                               {
                                   IDependency1 Dep1 { get; }

                                   IDependency2 Dep2 { get; }
                                   
                                   IDependency1 Dep3 { get; }
                               }

                               class Service: IService
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency1 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }

                                   public IDependency1 Dep1 { get; }

                                   public IDependency2 Dep2 { get; }
                                   
                                   public IDependency1 Dep3 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency1>().As(Lifetime.PerBlock).To<Dependency1>()
                                           .Bind<IDependency2>().As(Lifetime.PerBlock).To<Dependency2>()
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
                                       Console.WriteLine(service.Dep1 == service.Dep2.Dep1);
                                       Console.WriteLine(service.Dep1 == service.Dep3);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWhenWithinEnumerable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Collections.Generic;
                           using System.Linq;

                           namespace Sample
                           {
                               interface IDependency1 {}

                               class Dependency1: IDependency1
                               {
                                   public Dependency1()
                                   {
                                        Console.WriteLine("dep1");
                                   }
                               }

                               interface IDependency2
                               {
                                   IDependency1 Dep1 { get; }
                               }

                               class Dependency2: IDependency2
                               {
                                   public Dependency2(IDependency1 dep1) => Dep1 = dep1;

                                   public IDependency1 Dep1 { get; }
                               }

                               interface IService
                               {
                                   IDependency1 Dep1 { get; }

                                   IDependency2 Dep2 { get; }
                                   
                                   IDependency1 Dep3 { get; }
                               }

                               class Service: IService
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency1 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }

                                   public IDependency1 Dep1 { get; }

                                   public IDependency2 Dep2 { get; }
                                   
                                   public IDependency1 Dep3 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency1>().As(Lifetime.PerBlock).To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
                                           .Bind<IService>(1).To<Service>()
                                           .Bind<IService>(2).To<Service>()
                                           .Root<IEnumerable<IService>>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var services = composition.Service.ToArray();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["dep1"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolve()
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

                               class Dependency2: IDependency2
                               {
                                   public Dependency2(IDependency1 dep1) => Dep1 = dep1;

                                   public IDependency1 Dep1 { get; }
                               }

                               interface IService
                               {
                                   IDependency1 Dep1 { get; }

                                   IDependency2 Dep2 { get; }
                                   
                                   IDependency1 Dep3 { get; }
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency1 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }

                                   public IDependency1 Dep1 { get; }

                                   public IDependency2 Dep2 { get; }
                                   
                                   public IDependency1 Dep3 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency1>().As(Lifetime.PerResolve).To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
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
                                       Console.WriteLine(service.Dep1 == service.Dep2.Dep1);
                                       Console.WriteLine(service.Dep1 == service.Dep3);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveBinding()
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

                               class Dependency2: IDependency2
                               {
                                   public Dependency2(IDependency1 dep1) => Dep1 = dep1;

                                   public IDependency1 Dep1 { get; }
                               }

                               interface IService
                               {
                                   IDependency1 Dep1 { get; }

                                   IDependency2 Dep2 { get; }
                                   
                                   IDependency1 Dep3 { get; }
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency1 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }

                                   public IDependency1 Dep1 { get; }

                                   public IDependency2 Dep2 { get; }
                                   
                                   public IDependency1 Dep3 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .PerResolve<Dependency1>()
                                           .Transient<Dependency2>()
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
                                       Console.WriteLine(service.Dep1 == service.Dep2.Dep1);
                                       Console.WriteLine(service.Dep1 == service.Dep3);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWhenStatic()
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

                               class Dependency2: IDependency2
                               {
                                   public Dependency2(IDependency1 dep1) => Dep1 = dep1;

                                   public IDependency1 Dep1 { get; }
                               }

                               interface IService
                               {
                                   IDependency1 Dep1 { get; }

                                   IDependency2 Dep2 { get; }
                                   
                                   IDependency1 Dep3 { get; }
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency1 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }

                                   public IDependency1 Dep1 { get; }

                                   public IDependency2 Dep2 { get; }
                                   
                                   public IDependency1 Dep3 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency1>().As(Lifetime.PerResolve).To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service", kind: RootKinds.Static);
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = Composition.Service;
                                       Console.WriteLine(service.Dep1 == service.Dep2.Dep1);
                                       Console.WriteLine(service.Dep1 == service.Dep3);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Theory]
    [InlineData(Lifetime.Singleton)]
    [InlineData(Lifetime.Scoped)]
    [InlineData(Lifetime.PerResolve)]
    internal async Task ShouldSupportComplex(Lifetime lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Abc {}
                               
                               interface IDependency {}

                               class Dependency: IDependency
                               {
                                   public Dependency(Abc abc) { }
                               }

                               interface IService
                               {
                                   IDependency Dep1 { get; }

                                   IDependency Dep2 { get; }
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency dep1, IDependency dep2)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                   }

                                   public IDependency Dep1 { get; }

                                   public IDependency Dep2 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.#lifetime#).To<Abc>()
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
                                       Console.WriteLine(service.Dep1 == service.Dep2);
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Split().Count(i => i.TrimStart().StartsWith("EnsureDependencyExists")).ShouldBe(2, result);
    }

    [Theory]
    [InlineData(Lifetime.Singleton)]
    [InlineData(Lifetime.Scoped)]
    [InlineData(Lifetime.PerResolve)]
    internal async Task ShouldSupportComplexWhenFactoryMultiplyResolve(Lifetime lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Abc {}
                               
                               interface IDependency {}

                               class Dependency: IDependency
                               {
                                   public Dependency(Abc abc) { }
                               }

                               interface IService
                               {
                                   IDependency Dep1 { get; }

                                   IDependency Dep2 { get; }
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency dep1, IDependency dep2)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                   }

                                   public IDependency Dep1 { get; }

                                   public IDependency Dep2 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.#lifetime#).To<Abc>()
                                           .RootArg<bool>("flag")
                                           .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
                                           .Bind<IService>().To(ctx => {
                                                ctx.Inject(out bool flag);
                                                if (flag)
                                                {
                                                    ctx.Inject(out IDependency dep11);
                                                    ctx.Inject(out IDependency dep12);
                                                    return new Service(dep11, dep12);
                                                }
                                                else
                                                {
                                                    ctx.Inject(out IDependency dep21);
                                                    ctx.Inject(out IDependency dep22);
                                                    return new Service(dep21, dep22);
                                                }
                                           })
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service(false);
                                       Console.WriteLine(service.Dep1);
                                       Console.WriteLine(service.Dep1 == service.Dep2);
                                       var service2 = composition.Service(true);
                                       Console.WriteLine(service2.Dep1);
                                       Console.WriteLine(service2.Dep1 == service2.Dep2);
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Errors.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["Sample.Dependency", "True", "Sample.Dependency", "True"], result);
        result.GeneratedCode.Split().Count(i => i.TrimStart().StartsWith("EnsureDependencyExists")).ShouldBe(5, result);
    }

    [Theory]
    [InlineData(Lifetime.Singleton)]
    [InlineData(Lifetime.Scoped)]
    [InlineData(Lifetime.PerResolve)]
    internal async Task ShouldSupportComplexWhenFactoryMultiplyInjection(Lifetime lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Abc {}
                               
                               interface IDependency {}

                               class Dependency: IDependency
                               {
                                   public Dependency(Abc abc) { }
                               }

                               interface IService
                               {
                                   IDependency Dep1 { get; }

                                   IDependency Dep2 { get; }
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency dep1, IDependency dep2)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                   }

                                   public IDependency Dep1 { get; }

                                   public IDependency Dep2 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.#lifetime#).To<Abc>()
                                           .RootArg<bool>("flag")
                                           .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
                                           .Bind<IService>().To(ctx => {
                                                ctx.Inject(out bool flag);
                                                if (flag)
                                                {
                                                    ctx.Inject(out Service service1);
                                                    return service1;
                                                }
                                                else
                                                {
                                                    ctx.Inject(out Service service2);
                                                    return service2;
                                                }
                                           })
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service(false);
                                       Console.WriteLine(service.Dep1);
                                       Console.WriteLine(service.Dep1 == service.Dep2);
                                       var service2 = composition.Service(true);
                                       Console.WriteLine(service2.Dep1);
                                       Console.WriteLine(service2.Dep1 == service2.Dep2);
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Errors.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["Sample.Dependency", "True", "Sample.Dependency", "True"], result);
        result.GeneratedCode.Split().Count(i => i.TrimStart().StartsWith("EnsureDependencyExists")).ShouldBe(5, result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWhenStruct()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency1 {}

                               struct Dependency1: IDependency1 {}

                               interface IDependency2
                               {
                                   IDependency1 Dep1 { get; }
                               }

                               struct Dependency2: IDependency2
                               {
                                   public Dependency2(IDependency1 dep1) => Dep1 = dep1;

                                   public IDependency1 Dep1 { get; }
                               }

                               interface IService
                               {
                                   IDependency1 Dep1 { get; }

                                   IDependency2 Dep2 { get; }
                                   
                                   IDependency1 Dep3 { get; }
                               }

                               class Service: IService
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2, IDependency1 dep3)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                       Dep3 = dep3;
                                   }

                                   public IDependency1 Dep1 { get; }

                                   public IDependency2 Dep2 { get; }
                                   
                                   public IDependency1 Dep3 { get; }
                               }
                               
                               internal partial class Composition
                               {
                                   partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime) 
                                   {
                                       Console.WriteLine($"{typeof(T)} '{tag}' {lifetime} created");
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.OnNewInstance, "On")
                                           .Bind<IDependency1>().As(Lifetime.PerResolve).To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
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
        result.StdOut.ShouldBe(["Sample.Dependency1 '' PerResolve created", "Sample.Dependency2 '' Transient created", "Sample.Service '' Transient created"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWhenTypeImplementsSeveralGenericInterfaces()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency1<T> { }
                               
                               internal interface IDependency2<T> { }

                               internal class Dependency<T> : IDependency1<T>, IDependency2<T> { }

                               internal interface IService
                               {
                                   IDependency1<int> Dependency1 { get; }
                                           
                                   IDependency2<int> Dependency2 { get; }
                               }

                               internal class Service : IService
                               {
                                   public Service(IDependency1<int> dependency1, IDependency2<int> dependency2)
                                   {
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                   }

                                   public IDependency1<int> Dependency1 { get; }
                                           
                                   public IDependency2<int> Dependency2 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.PerResolve).To<Dependency<TT>>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependency1 == service.Dependency2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithinFunc()
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

                               interface ISin
                               {
                                   IDependency Dep { get; }
                               }

                               class Sin: ISin
                               {
                                   public Sin(IDependency dep)
                                   {
                                       Dep = dep;        
                                   }

                                   public IDependency Dep { get; }
                               }

                               interface IService
                               {        
                                   IDependency Dep { get; }

                                   ISin Sin { get; }
                               }

                               class Service: IService 
                               {
                                   private Func<IDependency> _dep;
                                   public Service(Func<IDependency> dep, ISin sin)
                                   {
                                       _dep = dep;
                                       Sin = sin;
                                   }

                                   public IDependency Dep => _dep();

                                   public ISin Sin { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
                                           .Bind<ISin>().To<Sin>()
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
                                       Console.WriteLine(service.Dep == service.Sin.Dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithinSingleton()
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

                               interface ISin
                               {
                                   IDependency Dep { get; }
                               }

                               class Sin: ISin
                               {
                                   public Sin(IDependency dep)
                                   {
                                       Dep = dep;        
                                   }

                                   public IDependency Dep { get; }
                               }

                               interface IService
                               {        
                                   IDependency Dep { get; }

                                   ISin Sin { get; }
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency dep, ISin sin)
                                   {
                                       Dep = dep;
                                       Sin = sin;
                                   }

                                   public IDependency Dep { get; }

                                   public ISin Sin { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
                                           .Bind<ISin>().As(Lifetime.Singleton).To<Sin>()
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
                                       var service2 = composition.Service;
                                       Console.WriteLine(service.Dep == service.Sin.Dep);        
                                       Console.WriteLine(service.Dep != service2.Dep);
                                       Console.WriteLine(service.Sin == service2.Sin);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportScoped()
    {
        // Given

        // When
        var result = await """
                               using System;
                               using Pure.DI;
                               using static Pure.DI.Lifetime; 

                               namespace Sample
                               {
                               interface ISingletonDep
                               {
                                   bool IsDisposed { get; }
                               }

                               class SingletonDep
                                   : ISingletonDep, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               interface IScopedDep
                               {
                                   ISingletonDep SingletonDep { get; }

                                   bool IsDisposed { get; }
                               }

                               class ScopedDep : IScopedDep, IDisposable
                               {
                                   private readonly ISingletonDep _singletonDep;

                                   public ScopedDep(ISingletonDep singletonDep)
                                   {
                                       _singletonDep = singletonDep;
                                   }

                                   public ISingletonDep SingletonDep => _singletonDep;
                                   
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               interface IService
                               {
                                   IScopedDep ScopedDep { get; }
                               }

                               class Service : IService
                               {
                                   private readonly IScopedDep _scopedDep;

                                   public Service(IScopedDep scopedDep)
                                   {
                                       _scopedDep = scopedDep;
                                   }

                                   public IScopedDep ScopedDep => _scopedDep;
                               }

                               // Implements a session
                               class Session : Composition
                               {
                                   public Session(Composition composition) : base(composition)
                                   {
                                   }
                               }

                               class ProgramRoot
                               {
                                   private readonly ISingletonDep _singletonDep;
                                   private readonly Func<Session> _sessionFactory;

                                   public ProgramRoot(ISingletonDep singletonDep,
                                       Func<Session> sessionFactory)
                                   {
                                       _singletonDep = singletonDep;
                                       _sessionFactory = sessionFactory;
                                   }

                                   public ISingletonDep SingletonDep => _singletonDep;
                                   
                                   public Session CreateSession() => _sessionFactory();
                               }

                               partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<ISingletonDep>()
                                               .As(Singleton)
                                               .To<SingletonDep>()
                                           .Bind<IScopedDep>()
                                               .As(Scoped)
                                               .To<ScopedDep>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("SessionRoot")
                                           .Root<ProgramRoot>("ProgramRoot");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var program = composition.ProgramRoot;

                                       // Creates session #1
                                       var session1 = program.CreateSession();
                                       var scopedDepInSession1 = session1.SessionRoot.ScopedDep;
                                       System.Console.WriteLine(scopedDepInSession1 == session1.SessionRoot.ScopedDep);

                                       // Checks that the singleton instances are identical
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep == program.SingletonDep);

                                       // Creates session #2
                                       var session2 = program.CreateSession();
                                       var scopedDepInSession2 = session2.SessionRoot.ScopedDep;
                                       System.Console.WriteLine(scopedDepInSession2 == session2.SessionRoot.ScopedDep);

                                       // Checks that the scoped instances are not identical in different sessions
                                       System.Console.WriteLine(scopedDepInSession1 != scopedDepInSession2);

                                       // Checks that the singleton instances are identical in different sessions
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep == scopedDepInSession2.SingletonDep);

                                       // Disposes of session #1
                                       session1.Dispose();
                                       // Checks that the scoped instance is finalized
                                       System.Console.WriteLine(scopedDepInSession1.IsDisposed);

                                       // Session #2 is still not finalized
                                       System.Console.WriteLine(session2.SessionRoot.ScopedDep.IsDisposed);

                                       // Disposes of session #2
                                       session2.Dispose();
                                       // Checks that the scoped instance is finalized
                                       System.Console.WriteLine(scopedDepInSession2.IsDisposed);

                                       // Disposes of composition
                                       composition.Dispose();
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep.IsDisposed);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True", "True", "False", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportScopedBinding()
    {
        // Given

        // When
        var result = await """
                               using System;
                               using Pure.DI;
                               using static Pure.DI.Lifetime; 

                               namespace Sample
                               {
                               interface ISingletonDep
                               {
                                   bool IsDisposed { get; }
                               }

                               class SingletonDep
                                   : ISingletonDep, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               interface IScopedDep
                               {
                                   ISingletonDep SingletonDep { get; }

                                   bool IsDisposed { get; }
                               }

                               class ScopedDep : IScopedDep, IDisposable
                               {
                                   private readonly ISingletonDep _singletonDep;

                                   public ScopedDep(ISingletonDep singletonDep)
                                   {
                                       _singletonDep = singletonDep;
                                   }

                                   public ISingletonDep SingletonDep => _singletonDep;
                                   
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               interface IService
                               {
                                   IScopedDep ScopedDep { get; }
                               }

                               class Service : IService
                               {
                                   private readonly IScopedDep _scopedDep;

                                   public Service(IScopedDep scopedDep)
                                   {
                                       _scopedDep = scopedDep;
                                   }

                                   public IScopedDep ScopedDep => _scopedDep;
                               }

                               // Implements a session
                               class Session : Composition
                               {
                                   public Session(Composition composition) : base(composition)
                                   {
                                   }
                               }

                               class ProgramRoot
                               {
                                   private readonly ISingletonDep _singletonDep;
                                   private readonly Func<Session> _sessionFactory;

                                   public ProgramRoot(ISingletonDep singletonDep,
                                       Func<Session> sessionFactory)
                                   {
                                       _singletonDep = singletonDep;
                                       _sessionFactory = sessionFactory;
                                   }

                                   public ISingletonDep SingletonDep => _singletonDep;
                                   
                                   public Session CreateSession() => _sessionFactory();
                               }

                               partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Singleton<SingletonDep>()
                                           .Scoped<ScopedDep>()
                                           .Transient<Service>()
                                           .Root<IService>("SessionRoot")
                                           .Root<ProgramRoot>("ProgramRoot");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var program = composition.ProgramRoot;

                                       // Creates session #1
                                       var session1 = program.CreateSession();
                                       var scopedDepInSession1 = session1.SessionRoot.ScopedDep;
                                       System.Console.WriteLine(scopedDepInSession1 == session1.SessionRoot.ScopedDep);

                                       // Checks that the singleton instances are identical
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep == program.SingletonDep);

                                       // Creates session #2
                                       var session2 = program.CreateSession();
                                       var scopedDepInSession2 = session2.SessionRoot.ScopedDep;
                                       System.Console.WriteLine(scopedDepInSession2 == session2.SessionRoot.ScopedDep);

                                       // Checks that the scoped instances are not identical in different sessions
                                       System.Console.WriteLine(scopedDepInSession1 != scopedDepInSession2);

                                       // Checks that the singleton instances are identical in different sessions
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep == scopedDepInSession2.SingletonDep);

                                       // Disposes of session #1
                                       session1.Dispose();
                                       // Checks that the scoped instance is finalized
                                       System.Console.WriteLine(scopedDepInSession1.IsDisposed);

                                       // Session #2 is still not finalized
                                       System.Console.WriteLine(session2.SessionRoot.ScopedDep.IsDisposed);

                                       // Disposes of session #2
                                       session2.Dispose();
                                       // Checks that the scoped instance is finalized
                                       System.Console.WriteLine(scopedDepInSession2.IsDisposed);

                                       // Disposes of composition
                                       composition.Dispose();
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep.IsDisposed);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True", "True", "False", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportScopedAsSingleton()
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
                                   public Service(Func<IDependency> dependency1, IDependency dependency2)
                                   {
                                       Dependency1 = dependency1();
                                       Dependency2 = dependency2;
                                   }

                                   public IDependency Dependency1 { get; }
                                           
                                   public IDependency Dependency2 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency>().As(Lifetime.Scoped).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IDependency>("Dependency")
                                           .Root<IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Root;
                                       var service2 = composition.Root;
                                       Console.WriteLine(service1.Dependency1 == service1.Dependency2);        
                                       Console.WriteLine(service2.Dependency1 == service1.Dependency1);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportSimpleSingleton()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IService>().As(Lifetime.Singleton).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSingleton()
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
                                   public Service(Func<IDependency> dependency1, IDependency dependency2)
                                   {
                                       Dependency1 = dependency1();
                                       Dependency2 = dependency2;
                                   }

                                   public IDependency Dependency1 { get; }
                                           
                                   public IDependency Dependency2 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IDependency>("Dependency")
                                           .Root<IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Root;
                                       var service2 = composition.Root;
                                       Console.WriteLine(service1.Dependency1 == service1.Dependency2);        
                                       Console.WriteLine(service2.Dependency1 == service1.Dependency1);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonBinding()
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
                               
                               internal interface IDependency2 { }

                               internal class Dependency2 : IDependency2 { }

                               internal interface IService
                               {
                                   public IDependency Dependency1 { get; }
                                           
                                   public IDependency Dependency2 { get; }
                               }

                               internal class Service : IService
                               {
                                   public Service(Func<IDependency> dependency1, IDependency dependency12, IDependency2 dep2, Dependency2 dep22)
                                   {
                                       Dependency1 = dependency1();
                                       Dependency2 = dependency12;
                                   }

                                   public IDependency Dependency1 { get; }
                                           
                                   public IDependency Dependency2 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Singleton<Dependency, Dependency2>()
                                           .Transient<Service>()
                                           .Root<IDependency>("Dependency")
                                           .Root<IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Root;
                                       var service2 = composition.Root;
                                       Console.WriteLine(service1.Dependency1 == service1.Dependency2);        
                                       Console.WriteLine(service2.Dependency1 == service1.Dependency1);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonComplex()
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

                               class Dependency2: IDependency2
                               {
                                   public Dependency2(IDependency1 dep1) => Dep1 = dep1;

                                   public IDependency1 Dep1 { get; }
                               }

                               interface IDependency3
                               {
                                   IDependency1 Dep1 { get; }

                                   IDependency2 Dep2 { get; }
                               }

                               class Dependency3: IDependency3
                               {
                                   public Dependency3(IDependency1 dep1, IDependency2 dep2)
                                   { 
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                   }       

                                   public IDependency1 Dep1 { get; }

                                   public IDependency2 Dep2 { get; }
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
                                       // FormatCode = On
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
                                       var composition = new Composition();
                                       var service1 = composition.Service;
                                       var service2 = composition.Service;
                                       Console.WriteLine(service1 != service2);        
                                       Console.WriteLine(service1.Dep1 != service2.Dep1);
                                       Console.WriteLine(service1.Dep2 == service2.Dep2);
                                       Console.WriteLine(service1.Dep2.Dep1 == service2.Dep2.Dep1);
                                       Console.WriteLine(service1.Dep2.Dep1 != service1.Dep1);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonWhenTypeImplementsSeveralGenericInterfaces()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency1<T> { }
                               
                               internal interface IDependency2<T> { }

                               internal class Dependency<T> : IDependency1<T>, IDependency2<T> { }

                               internal interface IService
                               {
                                   IDependency1<int> Dependency1 { get; }
                                           
                                   IDependency2<int> Dependency2 { get; }
                               }

                               internal class Service : IService
                               {
                                   public Service(IDependency1<int> dependency1, IDependency2<int> dependency2)
                                   {
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                   }

                                   public IDependency1<int> Dependency1 { get; }
                                           
                                   public IDependency2<int> Dependency2 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Dependency<TT>>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Root;
                                       var service2 = composition.Root;
                                       Console.WriteLine(service1.Dependency1 == service1.Dependency2);        
                                       Console.WriteLine(service2.Dependency1 == service1.Dependency1);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonWithinFunc()
    {
        // Given

        // When
        var result = await """
                               using System;
                               using System.Collections.Generic;
                               using Pure.DI;

                               namespace Sample
                               {
                                  interface IDep { }

                                   class Dep: IDep
                                   {
                                       public Dep(Func<int> val) { }
                                   }

                                   class Service
                                   {
                                       public Service(IEnumerable<IDep> deps) { }
                                   }

                                   static class Setup
                                   {
                                      private static void SetupComposition()
                                      {
                                          DI.Setup("Composition")
                                               .Hint(Hint.Resolve, "Off")
                                               .Hint(Hint.FormatCode, "On")
                                               .DefaultLifetime(Lifetime.Singleton)
                                               .Bind<int>().To(_ => 99)
                                               .Bind<IDep>().To<Dep>()
                                               .Root<Service>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportTransient()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IService>().As(Lifetime.Transient).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTransientBind()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Transient<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTransientBindWhenOneDependency()
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

                               interface IService {}
                               
                               class Service: IService
                               {
                                    public Service(IDependency dependency)
                                    {
                                    }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Transient<Dependency, Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTransientBindWhenTag()
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

                               interface IService {}
                               
                               class Service: IService
                               {
                                    public Service([Tag(123)] IDependency dependency)
                                    {
                                    }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Transient<Dependency, Service>("abc", 123)
                                           .Root<IService>("Service", "abc");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTransientBindWhenTwoDependencies()
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

                               interface IDependency2 {}
                               class Dependency2: IDependency2 {}

                               interface IService {}
                               
                               class Service: IService
                               {
                                    public Service(IDependency1 dependency1, IDependency2 dependency2)
                                    {
                                    }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Transient<Dependency1, Dependency2, Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTransientBindWhenFiveDependencies()
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
                               interface IDependency2 {}
                               class Dependency2: IDependency2 {}
                               interface IDependency3 {}
                               class Dependency3: IDependency3 {}
                               interface IDependency4 {}
                               class Dependency4: IDependency4 {}
                               interface IDependency5 {}
                               class Dependency5: IDependency5 {}

                               interface IService {}
                               
                               class Service: IService
                               {
                                    public Service(
                                        IDependency1 dependency1, 
                                        IDependency2 dependency2,
                                        IDependency3 dependency3,
                                        IDependency4 dependency4,
                                        IDependency5 dependency5)
                                    {
                                    }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Transient<Dependency1, Dependency2, Dependency3, Dependency4, Dependency5, Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTransientBindWhenTenDependencies()
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
                               interface IDependency2 {}
                               class Dependency2: IDependency2 {}
                               interface IDependency3 {}
                               class Dependency3: IDependency3 {}
                               interface IDependency4 {}
                               class Dependency4: IDependency4 {}
                               interface IDependency5 {}
                               class Dependency5: IDependency5 {}
                               interface IDependency6 {}
                               class Dependency6: IDependency6 {}
                               interface IDependency7 {}
                               class Dependency7: IDependency7 {}
                               interface IDependency8 {}
                               class Dependency8: IDependency8 {}
                               interface IDependency9 {}
                               class Dependency9: IDependency9 {}
                               interface IDependency10 {}
                               class Dependency10: IDependency10 {}

                               interface IService {}
                               
                               class Service: IService
                               {
                                    public Service(
                                        IDependency1 dependency1, 
                                        IDependency2 dependency2,
                                        IDependency3 dependency3,
                                        IDependency4 dependency4,
                                        IDependency5 dependency5,
                                        IDependency6 dependency6,
                                        IDependency7 dependency7,
                                        IDependency8 dependency8,
                                        IDependency9 dependency9,
                                        IDependency10 dependency10)
                                    {
                                    }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Transient<Dependency1, Dependency2, Dependency3, Dependency4, Dependency5, Dependency6, Dependency7, Dependency8, Dependency9, Dependency10, Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTransientWhenDefaultLifetimeWasDefined()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DefaultLifetime(Lifetime.Singleton)
                                           .Bind<IService>().As(Lifetime.Transient).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportValueTypeSingleton()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}

                               struct Dependency: IDependency 
                               {
                                   public Dependency()
                                   {
                                       Console.WriteLine("Dependency Created");
                                   }
                               }

                               interface IService
                               {
                                   IDependency Dep { get; }
                               }

                               class Service: IService 
                               {
                                   public Service(IDependency dep) => Dep = dep;

                                   public IDependency Dep { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
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
                                       var service3 = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency Created"], result);
    }

    [Fact]
    public async Task ShouldUseDefaultLifetimeForInterfaceWhenHasCommonDefaultLifetime()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime<IService>(Lifetime.Singleton)
                                           .DefaultLifetime(Lifetime.PerBlock)
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveInDeepHierarchy()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Level1 { public Level1(IDep d) {} }
                               class Level2 { public Level2(IDep d, Level1 l1) {} }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerResolve).To<Dep>()
                                           .Root<Level2>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithLazy()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IDep d1, Lazy<IDep> d2)
                                   {
                                       var val = d2.Value;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerResolve).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithFunc()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IDep d1, Func<IDep> d2)
                                   {
                                       var val = d2();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerResolve).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithLazy()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IDep d1, Lazy<IDep> d2)
                                   {
                                       var val = d2.Value;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerBlock).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithFunc()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IDep d1, Func<IDep> d2)
                                   {
                                       var val = d2();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerBlock).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithGeneric()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep<T> {}
                               class Dep<T>: IDep<T> { public Dep() => Console.WriteLine(typeof(T).Name); }

                               class Service
                               {
                                   public Service(IDep<int> d1, IDep<int> d2, IDep<string> d3) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep<TT>>().As(Lifetime.PerResolve).To<Dep<TT>>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32", "String"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithGeneric()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep<T> {}
                               class Dep<T>: IDep<T> { public Dep() => Console.WriteLine(typeof(T).Name); }

                               class Service
                               {
                                   public Service(IDep<int> d1, IDep<int> d2, IDep<string> d3) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep<TT>>().As(Lifetime.PerBlock).To<Dep<TT>>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32", "String"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonWithGeneric()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep<T> {}
                               class Dep<T>: IDep<T> { public Dep() => Console.WriteLine(typeof(T).Name); }

                               class Service
                               {
                                   public Service(IDep<int> d1, IDep<int> d2, IDep<string> d3) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep<TT>>().As(Lifetime.Singleton).To<Dep<TT>>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r1 = composition.Root;
                                       var r2 = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32", "String"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithTags()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service([Tag(1)] IDep d1, [Tag(1)] IDep d2, [Tag(2)] IDep d3) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>(1, 2).As(Lifetime.PerResolve).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithTags()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service([Tag(1)] IDep d1, [Tag(1)] IDep d2, [Tag(2)] IDep d3) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>(1, 2).As(Lifetime.PerBlock).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonWithTags()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service([Tag(1)] IDep d1, [Tag(1)] IDep d2, [Tag(2)] IDep d3) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>(1, 2).As(Lifetime.Singleton).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r1 = composition.Root;
                                       var r2 = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IDep d1, IDep d2) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerResolve).To(ctx => new Dep())
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IDep d1, IDep d2) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerBlock).To(ctx => new Dep())
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonWithFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IDep d1, IDep d2) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.Singleton).To(ctx => new Dep())
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r1 = composition.Root;
                                       var r2 = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithArray()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IDep[] d1, IDep[] d2) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerResolve).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithArray()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IDep[] d1, IDep[] d2) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerBlock).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonWithArray()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IDep[] d1, IDep[] d2) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.Singleton).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r1 = composition.Root;
                                       var r2 = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithEnumerable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IEnumerable<IDep> d1, IEnumerable<IDep> d2) 
                                   {
                                       foreach(var x in d1) {}
                                       foreach(var x in d2) {}
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerResolve).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithEnumerable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IEnumerable<IDep> d1, IEnumerable<IDep> d2) 
                                   {
                                       foreach(var x in d1) {}
                                       foreach(var x in d2) {}
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerBlock).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor", "Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonWithEnumerable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(IEnumerable<IDep> d1, IEnumerable<IDep> d2) 
                                   {
                                       foreach(var x in d1) {}
                                       foreach(var x in d2) {}
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.Singleton).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r1 = composition.Root;
                                       var r2 = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWhenCircularDependencyViaLazy()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep1 {}
                               class Dep1: IDep1 { public Dep1(Lazy<IDep2> d2) {} }

                               interface IDep2 {}
                               class Dep2: IDep2 { public Dep2(IDep1 d1) {} }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep1>().As(Lifetime.PerResolve).To<Dep1>()
                                           .Bind<IDep2>().As(Lifetime.PerResolve).To<Dep2>()
                                           .Root<IDep1>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithSeveralRoots()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>("Root1", "Root2").As(Lifetime.PerResolve).To<Dep>()
                                           .Root<IDep>("Root1", "Root1")
                                           .Root<IDep>("Root2", "Root2");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r1 = composition.Root1;
                                       var r2 = composition.Root2;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor", "Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithSeveralRoots()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>("Root1", "Root2").As(Lifetime.PerBlock).To<Dep>()
                                           .Root<IDep>("Root1", "Root1")
                                           .Root<IDep>("Root2", "Root2");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r1 = composition.Root1;
                                       var r2 = composition.Root2;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor", "Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonWithSeveralRoots()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>("Root1", "Root2").As(Lifetime.Singleton).To<Dep>()
                                           .Root<IDep>("Root1", "Root1")
                                           .Root<IDep>("Root2", "Root2");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r1 = composition.Root1;
                                       var r2 = composition.Root2;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithTask()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(Task<IDep> d1, Task<IDep> d2) 
                                   {
                                       var v1 = d1.Result;
                                       var v2 = d2.Result;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerResolve).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerResolveWithValueTask()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep { public Dep() => Console.WriteLine("Dep Ctor"); }

                               class Service
                               {
                                   public Service(ValueTask<IDep> d1, ValueTask<IDep> d2) 
                                   {
                                       var v1 = d1.Result;
                                       var v2 = d2.Result;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().As(Lifetime.PerResolve).To<Dep>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var r = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dep Ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithMultipleFuncDifferentTypes()
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

                               interface IDependency2 {}
                               class Dependency2: IDependency2 {}

                               interface IDependency3 {}
                               class Dependency3: IDependency3 {}

                               interface IService
                               {
                                   IDependency1 Dep1 { get; }
                                   IDependency2 Dep2 { get; }
                                   IDependency3 Dep3 { get; }
                               }

                               class Service: IService
                               {
                                   public Service(
                                       Func<IDependency1> dep1,
                                       Func<IDependency2> dep2,
                                       Func<IDependency3> dep3)
                                   {
                                       Dep1 = dep1();
                                       Dep2 = dep2();
                                       Dep3 = dep3();
                                   }

                                   public IDependency1 Dep1 { get; }
                                   public IDependency2 Dep2 { get; }
                                   public IDependency3 Dep3 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDependency1>().To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
                                           .Bind<IDependency3>().To<Dependency3>()
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithMultipleNestedFuncScopes()
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

                               interface IDependency2 {}
                               class Dependency2: IDependency2 {}

                               interface IService
                               {
                                   IDependency1 Dep1 { get; }
                                   IDependency2 Dep2 { get; }
                               }

                               class Service: IService
                               {
                                   public Service(
                                       Func<IDependency1> dep1Factory,
                                       Func<IDependency2> dep2Factory)
                                   {
                                       Dep1 = dep1Factory();
                                       Dep2 = dep2Factory();
                                       var dep1Again = dep1Factory();
                                       var dep2Again = dep2Factory();
                                   }

                                   public IDependency1 Dep1 { get; }
                                   public IDependency2 Dep2 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDependency1>().To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithNestedFuncCreatingObjectsWithFunc()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep1 {}
                               class Dep1: IDep1 {}

                               interface IDep2 {}
                               class Dep2: IDep2 {}

                               interface IDep3 {}
                               class Dep3: IDep3 {}

                               interface ISvc1 {}
                               class Svc1: ISvc1 {}

                               interface ISvc2 {}
                               class Svc2: ISvc2 {}

                               interface ISvc3 {}
                               class Svc3: ISvc3 {}

                               class MidService
                               {
                                   public MidService(
                                       Func<IDep1> dep1Factory,
                                       Func<IDep2> dep2Factory,
                                       Func<ISvc1> svc1Factory)
                                   {
                                       var d1 = dep1Factory();
                                       var d2 = dep2Factory();
                                       var s1 = svc1Factory();
                                   }
                               }

                               class HighService
                               {
                                   public HighService(
                                       Func<IDep3> dep3Factory,
                                       Func<MidService> midFactory,
                                       Func<ISvc2> svc2Factory,
                                       Func<ISvc3> svc3Factory)
                                   {
                                       var d3 = dep3Factory();
                                       var mid1 = midFactory();
                                       var mid2 = midFactory();
                                       var s2 = svc2Factory();
                                       var s3 = svc3Factory();
                                   }
                               }

                               class RootService
                               {
                                   public RootService(Func<HighService> highFactory)
                                   {
                                       var h1 = highFactory();
                                       var h2 = highFactory();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDep1>().To<Dep1>()
                                           .Bind<IDep2>().To<Dep2>()
                                           .Bind<IDep3>().To<Dep3>()
                                           .Bind<ISvc1>().To<Svc1>()
                                           .Bind<ISvc2>().To<Svc2>()
                                           .Bind<ISvc3>().To<Svc3>()
                                           .Bind<MidService>().To<MidService>()
                                           .Bind<HighService>().To<HighService>()
                                           .Bind<RootService>().To<RootService>()
                                           .Root<RootService>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithFactoryInjectingMultipleTypesWithSameFunc()
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

                               interface IService1 {}
                               class Service1: IService1
                               {
                                   public Service1(Func<IDependency> depFactory)
                                   {
                                       Dep = depFactory();
                                   }

                                   public IDependency Dep { get; }
                               }

                               interface IService2 {}
                               class Service2: IService2
                               {
                                   public Service2(Func<IDependency> depFactory)
                                   {
                                       Dep = depFactory();
                                   }

                                   public IDependency Dep { get; }
                               }

                               interface IService3 {}
                               class Service3: IService3
                               {
                                   public Service3(Func<IDependency> depFactory)
                                   {
                                       Dep = depFactory();
                                   }

                                   public IDependency Dep { get; }
                               }

                               class CompositeService
                               {
                                   public CompositeService(IService1 service1, IService2 service2, IService3 service3)
                                   {
                                       Service1 = service1;
                                       Service2 = service2;
                                       Service3 = service3;
                                   }

                                   public IService1 Service1 { get; }
                                   public IService2 Service2 { get; }
                                   public IService3 Service3 { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService1>().To<Service1>()
                                           .Bind<IService2>().To<Service2>()
                                           .Bind<IService3>().To<Service3>()
                                           .Bind<CompositeService>()
                                               .To(ctx =>
                                               {
                                                   ctx.Inject<IService1>(out var svc1);
                                                   ctx.Inject<IService2>(out var svc2);
                                                   ctx.Inject<IService3>(out var svc3);
                                                   return new CompositeService(svc1, svc2, svc3);
                                               })
                                           .Root<CompositeService>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithNestedFactoryCreatingSameFuncMultipleTimes()
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

                               class Service1
                               {
                                   public Service1(Func<IDependency> depFactory)
                                   {
                                       Dep1 = depFactory();
                                       Dep2 = depFactory();
                                   }

                                   public IDependency Dep1 { get; }
                                   public IDependency Dep2 { get; }
                               }

                               class Service2
                               {
                                   public Service2(Func<IDependency> depFactory)
                                   {
                                       Dep1 = depFactory();
                                       Dep2 = depFactory();
                                   }

                                   public IDependency Dep1 { get; }
                                   public IDependency Dep2 { get; }
                               }

                               class RootService
                               {
                                   public RootService(
                                       Func<Service1> service1Factory,
                                       Func<Service2> service2Factory)
                                   {
                                       var svc1a = service1Factory();
                                       var svc1b = service1Factory();
                                       var svc2a = service2Factory();
                                       var svc2b = service2Factory();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDependency>().As(Lifetime.PerBlock).To<Dependency>()
                                           .Bind<Service1>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDependency>>(out var depFactory);
                                                   return new Service1(depFactory);
                                               })
                                           .Bind<Service2>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDependency>>(out var depFactory);
                                                   return new Service2(depFactory);
                                               })
                                           .Bind<RootService>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<Service1>>(out var svc1Factory);
                                                   ctx.Inject<Func<Service2>>(out var svc2Factory);
                                                   return new RootService(svc1Factory, svc2Factory);
                                               })
                                           .Root<RootService>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithComplexNestedFactoriesUsingSameFunc()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDepA {}
                               class DepA: IDepA {}

                               interface IDepB {}
                               class DepB: IDepB {}

                               interface IDepC {}
                               class DepC: IDepC {}

                               interface ISvcA {}
                               class SvcA: ISvcA
                               {
                                   public SvcA(Func<IDepA> depAFactory, Func<IDepB> depBFactory)
                                   {
                                       DepA = depAFactory();
                                       DepB = depBFactory();
                                   }

                                   public IDepA DepA { get; }
                                   public IDepB DepB { get; }
                               }

                               interface ISvcB {}
                               class SvcB: ISvcB
                               {
                                   public SvcB(Func<IDepB> depBFactory, Func<IDepC> depCFactory)
                                   {
                                       DepB = depBFactory();
                                       DepC = depCFactory();
                                   }

                                   public IDepB DepB { get; }
                                   public IDepC DepC { get; }
                               }

                               interface ISvcC {}
                               class SvcC: ISvcC
                               {
                                   public SvcC(Func<IDepA> depAFactory, Func<IDepC> depCFactory)
                                   {
                                       DepA = depAFactory();
                                       DepC = depCFactory();
                                   }

                                   public IDepA DepA { get; }
                                   public IDepC DepC { get; }
                               }

                               class CompositeService
                               {
                                   public CompositeService(
                                       Func<ISvcA> svcAFactory,
                                       Func<ISvcB> svcBFactory,
                                       Func<ISvcC> svcCFactory)
                                   {
                                       var sa1 = svcAFactory();
                                       var sb1 = svcBFactory();
                                       var sc1 = svcCFactory();
                                       var sa2 = svcAFactory();
                                       var sb2 = svcBFactory();
                                       var sc2 = svcCFactory();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDepA>().As(Lifetime.PerBlock).To<DepA>()
                                           .Bind<IDepB>().As(Lifetime.PerBlock).To<DepB>()
                                           .Bind<IDepC>().As(Lifetime.PerBlock).To<DepC>()
                                           .Bind<ISvcA>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDepA>>(out var depAFact);
                                                   ctx.Inject<Func<IDepB>>(out var depBFact);
                                                   return new SvcA(depAFact, depBFact);
                                               })
                                           .Bind<ISvcB>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDepB>>(out var depBFact);
                                                   ctx.Inject<Func<IDepC>>(out var depCFact);
                                                   return new SvcB(depBFact, depCFact);
                                               })
                                           .Bind<ISvcC>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDepA>>(out var depAFact);
                                                   ctx.Inject<Func<IDepC>>(out var depCFact);
                                                   return new SvcC(depAFact, depCFact);
                                               })
                                           .Bind<CompositeService>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<ISvcA>>(out var svcAFact);
                                                   ctx.Inject<Func<ISvcB>>(out var svcBFact);
                                                   ctx.Inject<Func<ISvcC>>(out var svcCFact);
                                                   return new CompositeService(svcAFact, svcBFact, svcCFact);
                                               })
                                           .Root<CompositeService>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockWithDeeplyNestedFuncFactories()
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

                               class Level1
                               {
                                   public Level1(Func<IDependency> depFactory)
                                   {
                                       Dep = depFactory();
                                   }

                                   public IDependency Dep { get; }
                               }

                               class Level2
                               {
                                   public Level2(Func<Level1> level1Factory)
                                   {
                                       L1 = level1Factory();
                                   }

                                   public Level1 L1 { get; }
                               }

                               class Level3
                               {
                                   public Level3(Func<Level2> level2Factory)
                                   {
                                       L2 = level2Factory();
                                   }

                                   public Level2 L2 { get; }
                               }

                               class Root
                               {
                                   public Root(
                                       Func<Level1> l1Factory,
                                       Func<Level2> l2Factory,
                                       Func<Level3> l3Factory)
                                   {
                                       var l1a = l1Factory();
                                       var l1b = l1Factory();
                                       var l2a = l2Factory();
                                       var l2b = l2Factory();
                                       var l3a = l3Factory();
                                       var l3b = l3Factory();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDependency>().As(Lifetime.PerBlock).To<Dependency>()
                                           .Bind<Level1>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDependency>>(out var depFactory);
                                                   return new Level1(depFactory);
                                               })
                                           .Bind<Level2>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<Level1>>(out var l1Factory);
                                                   return new Level2(l1Factory);
                                               })
                                           .Bind<Level3>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<Level2>>(out var l2Factory);
                                                   return new Level3(l2Factory);
                                               })
                                           .Bind<Root>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<Level1>>(out var l1Factory);
                                                   ctx.Inject<Func<Level2>>(out var l2Factory);
                                                   ctx.Inject<Func<Level3>>(out var l3Factory);
                                                   return new Root(l1Factory, l2Factory, l3Factory);
                                               })
                                           .Root<Root>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockFuncWithDifferentLifetimesInChain()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDepTransient {}
                               class DepTransient: IDepTransient {}

                               interface IDepSingleton {}
                               class DepSingleton: IDepSingleton {}

                               interface IDepScoped {}
                               class DepScoped: IDepScoped {}

                               class Service
                               {
                                   public Service(
                                       Func<IDepTransient> transientFactory,
                                       Func<IDepSingleton> singletonFactory,
                                       Func<IDepScoped> scopedFactory)
                                   {
                                       var t = transientFactory();
                                       var s = singletonFactory();
                                       var sc = scopedFactory();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDepTransient>().As(Lifetime.Transient).To<DepTransient>()
                                           .Bind<IDepSingleton>().As(Lifetime.Singleton).To<DepSingleton>()
                                           .Bind<IDepScoped>().As(Lifetime.Scoped).To<DepScoped>()
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockFuncWithEnumerable()
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
                               class Dependency: IDependency {}

                               class Service
                               {
                                   public Service(
                                       Func<IDependency> depFactory1,
                                       Func<IDependency> depFactory2,
                                       Func<IDependency> depFactory3)
                                   {
                                       var d1 = depFactory1();
                                       var d2 = depFactory2();
                                       var d3 = depFactory3();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockFuncWithLazyAndDifferentLifetimes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep1 {}
                               class Dep1: IDep1 {}

                               interface IDep2 {}
                               class Dep2: IDep2 {}

                               interface IDep3 {}
                               class Dep3: IDep3 {}

                               class Service1
                               {
                                   public Service1(
                                       Lazy<IDep1> lazyDep1,
                                       Func<IDep2> dep2Factory,
                                       Lazy<Func<IDep3>> lazyDep3Factory)
                                   {
                                       var d1 = lazyDep1.Value;
                                       var d2 = dep2Factory();
                                       var d3 = lazyDep3Factory.Value();
                                   }
                               }

                               class Service2
                               {
                                   public Service2(Func<IDep1> dep1Factory, Lazy<IDep2> lazyDep2)
                                   {
                                       var d1 = dep1Factory();
                                       var d2 = lazyDep2.Value;
                                   }
                               }

                               class Root
                               {
                                   public Root(Service1 svc1, Service2 svc2)
                                   {
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDep1>().As(Lifetime.Singleton).To<Dep1>()
                                           .Bind<IDep2>().As(Lifetime.Transient).To<Dep2>()
                                           .Bind<IDep3>().As(Lifetime.Scoped).To<Dep3>()
                                           .Bind<Service1>().To<Service1>()
                                           .Bind<Service2>().To<Service2>()
                                           .Bind<Root>().To<Root>()
                                           .Root<Root>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockFuncWithPerResolveDependencies()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep1 {}
                               class Dep1: IDep1 {}

                               interface IDep2 {}
                               class Dep2: IDep2 {}

                               interface IDep3 {}
                               class Dep3: IDep3 {}

                               class Service
                               {
                                   public Service(
                                       Func<IDep1> dep1Factory,
                                       Func<IDep2> dep2Factory,
                                       Func<IDep3> dep3Factory,
                                       IDep1 directDep1,
                                       IDep2 directDep2)
                                   {
                                       var d1 = dep1Factory();
                                       var d2 = dep2Factory();
                                       var d3 = dep3Factory();
                                       var dd1 = directDep1;
                                       var dd2 = directDep2;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDep1>().As(Lifetime.PerResolve).To<Dep1>()
                                           .Bind<IDep2>().As(Lifetime.Singleton).To<Dep2>()
                                           .Bind<IDep3>().As(Lifetime.Transient).To<Dep3>()
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockFuncWhenUsedInsideSingletonBlockAndRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep {}

                               interface IDep2 {}
                               class Dep2: IDep2 {}

                               class SingletonService
                               {
                                   public SingletonService(Func<IDep> depFact, IDep2 dep2)
                                   {
                                       var d1 = depFact();
                                       var d2 = depFact();
                                       _ = dep2;
                                   }
                               }

                               class Root
                               {
                                   public Root(Func<IDep> depFact, SingletonService singleton)
                                   {
                                       var d1 = depFact();
                                       var d2 = depFact();
                                       _ = singleton;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDep>().To<Dep>()
                                           .Bind<IDep2>().To<Dep2>()
                                           .Bind<SingletonService>().As(Lifetime.Singleton).To<SingletonService>()
                                           .Bind<Root>().To<Root>()
                                           .Root<Root>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       _ = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockFuncWithMultipleRoots()
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

                               interface IService1 {}
                               class Service1: IService1
                               {
                                   public Service1(Func<IDependency> depFactory)
                                   {
                                       Dep = depFactory();
                                   }

                                   public IDependency Dep { get; }
                               }

                               interface IService2 {}
                               class Service2: IService2
                               {
                                   public Service2(Func<IDependency> depFactory)
                                   {
                                       Dep = depFactory();
                                   }

                                   public IDependency Dep { get; }
                               }

                               interface IService3 {}
                               class Service3: IService3
                               {
                                   public Service3(Func<IDependency> depFactory)
                                   {
                                       Dep = depFactory();
                                   }

                                   public IDependency Dep { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService1>().To<Service1>()
                                           .Bind<IService2>().To<Service2>()
                                           .Bind<IService3>().To<Service3>()
                                           .Root<IService1>("Service1")
                                           .Root<IService2>("Service2")
                                           .Root<IService3>("Service3");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Service1;
                                       var service2 = composition.Service2;
                                       var service3 = composition.Service3;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportPerBlockFuncWithVeryComplexNestedFactories()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Collections.Generic;

                           namespace Sample
                           {
                               interface IDep1 {}
                               class Dep1: IDep1 {}

                               interface IDep2 {}
                               class Dep2: IDep2 {}

                               interface IDep3 {}
                               class Dep3: IDep3 {}

                               interface IDep4 {}
                               class Dep4: IDep4 {}

                               interface IDep5 {}
                               class Dep5: IDep5 {}

                               class MidService1
                               {
                                   public MidService1(
                                       Func<IDep1> d1Fact,
                                       Func<IDep2> d2Fact,
                                       Func<IDep3> d3Fact)
                                   {
                                       var a = d1Fact();
                                       var b = d2Fact();
                                       var c = d3Fact();
                                       var d = d1Fact();
                                       var e = d2Fact();
                                       var f = d3Fact();
                                   }
                               }

                               class MidService2
                               {
                                   public MidService2(
                                       Func<IDep3> d3Fact,
                                       Func<IDep4> d4Fact,
                                       Func<IDep5> d5Fact)
                                   {
                                       var a = d3Fact();
                                       var b = d4Fact();
                                       var c = d5Fact();
                                       var d = d3Fact();
                                       var e = d4Fact();
                                       var f = d5Fact();
                                   }
                               }

                               class MidService3
                               {
                                   public MidService3(
                                       Func<IDep1> d1Fact,
                                       Func<IDep4> d4Fact,
                                       Func<IDep5> d5Fact)
                                   {
                                       var a = d1Fact();
                                       var b = d4Fact();
                                       var c = d5Fact();
                                       var d = d1Fact();
                                       var e = d4Fact();
                                       var f = d5Fact();
                                   }
                               }

                               class HighService1
                               {
                                   public HighService1(
                                       Func<MidService1> ms1Fact,
                                       Func<MidService2> ms2Fact)
                                   {
                                       var x = ms1Fact();
                                       var y = ms2Fact();
                                       var z = ms1Fact();
                                       var w = ms2Fact();
                                   }
                               }

                               class HighService2
                               {
                                   public HighService2(
                                       Func<MidService2> ms2Fact,
                                       Func<MidService3> ms3Fact)
                                   {
                                       var x = ms2Fact();
                                       var y = ms3Fact();
                                       var z = ms2Fact();
                                       var w = ms3Fact();
                                   }
                               }

                               class RootService
                               {
                                   public RootService(
                                       Func<HighService1> hs1Fact,
                                       Func<HighService2> hs2Fact)
                                   {
                                       var a = hs1Fact();
                                       var b = hs2Fact();
                                       var c = hs1Fact();
                                       var d = hs2Fact();
                                       var e = hs1Fact();
                                       var f = hs2Fact();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDep1>().To<Dep1>()
                                           .Bind<IDep2>().To<Dep2>()
                                           .Bind<IDep3>().To<Dep3>()
                                           .Bind<IDep4>().To<Dep4>()
                                           .Bind<IDep5>().To<Dep5>()
                                           .Bind<MidService1>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDep1>>(out var d1Fact);
                                                   ctx.Inject<Func<IDep2>>(out var d2Fact);
                                                   ctx.Inject<Func<IDep3>>(out var d3Fact);
                                                   return new MidService1(d1Fact, d2Fact, d3Fact);
                                               })
                                           .Bind<MidService2>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDep3>>(out var d3Fact);
                                                   ctx.Inject<Func<IDep4>>(out var d4Fact);
                                                   ctx.Inject<Func<IDep5>>(out var d5Fact);
                                                   return new MidService2(d3Fact, d4Fact, d5Fact);
                                               })
                                           .Bind<MidService3>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDep1>>(out var d1Fact);
                                                   ctx.Inject<Func<IDep4>>(out var d4Fact);
                                                   ctx.Inject<Func<IDep5>>(out var d5Fact);
                                                   return new MidService3(d1Fact, d4Fact, d5Fact);
                                               })
                                           .Bind<HighService1>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<MidService1>>(out var ms1Fact);
                                                   ctx.Inject<Func<MidService2>>(out var ms2Fact);
                                                   return new HighService1(ms1Fact, ms2Fact);
                                               })
                                           .Bind<HighService2>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<MidService2>>(out var ms2Fact);
                                                   ctx.Inject<Func<MidService3>>(out var ms3Fact);
                                                   return new HighService2(ms2Fact, ms3Fact);
                                               })
                                           .Bind<RootService>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<HighService1>>(out var hs1Fact);
                                                   ctx.Inject<Func<HighService2>>(out var hs2Fact);
                                                   return new RootService(hs1Fact, hs2Fact);
                                               })
                                           .Root<RootService>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockFuncWithDeeplyNestedLocalFunctions()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep1 {}
                               class Dep1: IDep1 {}

                               interface IDep2 {}
                               class Dep2: IDep2 {}

                               interface IDep3 {}
                               class Dep3: IDep3 {}

                               interface IDep4 {}
                               class Dep4: IDep4 {}

                               interface IDep5 {}
                               class Dep5: IDep5 {}

                               interface IDep6 {}
                               class Dep6: IDep6 {}

                               interface IDep7 {}
                               class Dep7: IDep7 {}

                               interface IDep8 {}
                               class Dep8: IDep8 {}

                               class Level1Service
                               {
                                   public Level1Service(Func<IDep1> d1Fact, Func<IDep2> d2Fact)
                                   {
                                       var a = d1Fact();
                                       var b = d2Fact();
                                       var c = d1Fact();
                                       var d = d2Fact();
                                   }
                               }

                               class Level2Service
                               {
                                   public Level2Service(
                                       Func<IDep3> d3Fact,
                                       Func<IDep4> d4Fact,
                                       Func<Level1Service> l1Fact)
                                   {
                                       var a = d3Fact();
                                       var b = d4Fact();
                                       var c = l1Fact();
                                       var d = d3Fact();
                                       var e = d4Fact();
                                       var f = l1Fact();
                                   }
                               }

                               class Level3Service
                               {
                                   public Level3Service(
                                       Func<IDep5> d5Fact,
                                       Func<IDep6> d6Fact,
                                       Func<Level2Service> l2Fact)
                                   {
                                       var a = d5Fact();
                                       var b = d6Fact();
                                       var c = l2Fact();
                                       var d = d5Fact();
                                       var e = d6Fact();
                                       var f = l2Fact();
                                   }
                               }

                               class Level4Service
                               {
                                   public Level4Service(
                                       Func<IDep7> d7Fact,
                                       Func<IDep8> d8Fact,
                                       Func<Level3Service> l3Fact)
                                   {
                                       var a = d7Fact();
                                       var b = d8Fact();
                                       var c = l3Fact();
                                       var d = d7Fact();
                                       var e = d8Fact();
                                       var f = l3Fact();
                                   }
                               }

                               class Level5Service
                               {
                                   public Level5Service(
                                       Func<Level1Service> l1Fact,
                                       Func<Level2Service> l2Fact,
                                       Func<Level3Service> l3Fact,
                                       Func<Level4Service> l4Fact)
                                   {
                                       var a = l1Fact();
                                       var b = l2Fact();
                                       var c = l3Fact();
                                       var d = l4Fact();
                                       var e = l1Fact();
                                       var f = l2Fact();
                                       var g = l3Fact();
                                       var h = l4Fact();
                                   }
                               }

                               class Level6Service
                               {
                                   public Level6Service(Func<Level5Service> l5Fact)
                                   {
                                       var a = l5Fact();
                                       var b = l5Fact();
                                       var c = l5Fact();
                                       var d = l5Fact();
                                       var e = l5Fact();
                                       var f = l5Fact();
                                   }
                               }

                               class RootService
                               {
                                   public RootService(
                                       Func<Level1Service> l1Fact,
                                       Func<Level2Service> l2Fact,
                                       Func<Level3Service> l3Fact,
                                       Func<Level4Service> l4Fact,
                                       Func<Level5Service> l5Fact,
                                       Func<Level6Service> l6Fact)
                                   {
                                       var a = l1Fact();
                                       var b = l2Fact();
                                       var c = l3Fact();
                                       var d = l4Fact();
                                       var e = l5Fact();
                                       var f = l6Fact();
                                       var g = l1Fact();
                                       var h = l2Fact();
                                       var i = l3Fact();
                                       var j = l4Fact();
                                       var k = l5Fact();
                                       var l = l6Fact();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDep1>().To<Dep1>()
                                           .Bind<IDep2>().To<Dep2>()
                                           .Bind<IDep3>().To<Dep3>()
                                           .Bind<IDep4>().To<Dep4>()
                                           .Bind<IDep5>().To<Dep5>()
                                           .Bind<IDep6>().To<Dep6>()
                                           .Bind<IDep7>().To<Dep7>()
                                           .Bind<IDep8>().To<Dep8>()
                                           .Bind<Level1Service>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDep1>>(out var d1Fact);
                                                   ctx.Inject<Func<IDep2>>(out var d2Fact);
                                                   return new Level1Service(d1Fact, d2Fact);
                                               })
                                           .Bind<Level2Service>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDep3>>(out var d3Fact);
                                                   ctx.Inject<Func<IDep4>>(out var d4Fact);
                                                   ctx.Inject<Func<Level1Service>>(out var l1Fact);
                                                   return new Level2Service(d3Fact, d4Fact, l1Fact);
                                               })
                                           .Bind<Level3Service>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDep5>>(out var d5Fact);
                                                   ctx.Inject<Func<IDep6>>(out var d6Fact);
                                                   ctx.Inject<Func<Level2Service>>(out var l2Fact);
                                                   return new Level3Service(d5Fact, d6Fact, l2Fact);
                                               })
                                           .Bind<Level4Service>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDep7>>(out var d7Fact);
                                                   ctx.Inject<Func<IDep8>>(out var d8Fact);
                                                   ctx.Inject<Func<Level3Service>>(out var l3Fact);
                                                   return new Level4Service(d7Fact, d8Fact, l3Fact);
                                               })
                                           .Bind<Level5Service>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<Level1Service>>(out var l1Fact);
                                                   ctx.Inject<Func<Level2Service>>(out var l2Fact);
                                                   ctx.Inject<Func<Level3Service>>(out var l3Fact);
                                                   ctx.Inject<Func<Level4Service>>(out var l4Fact);
                                                   return new Level5Service(l1Fact, l2Fact, l3Fact, l4Fact);
                                               })
                                           .Bind<Level6Service>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<Level5Service>>(out var l5Fact);
                                                   return new Level6Service(l5Fact);
                                               })
                                           .Bind<RootService>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<Level1Service>>(out var l1Fact);
                                                   ctx.Inject<Func<Level2Service>>(out var l2Fact);
                                                   ctx.Inject<Func<Level3Service>>(out var l3Fact);
                                                   ctx.Inject<Func<Level4Service>>(out var l4Fact);
                                                   ctx.Inject<Func<Level5Service>>(out var l5Fact);
                                                   ctx.Inject<Func<Level6Service>>(out var l6Fact);
                                                   return new RootService(l1Fact, l2Fact, l3Fact, l4Fact, l5Fact, l6Fact);
                                               })
                                           .Root<RootService>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockFuncWithManyFuncsInOneConstructor()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep1 {}
                               class Dep1: IDep1 {}

                               interface IDep2 {}
                               class Dep2: IDep2 {}

                               interface IDep3 {}
                               class Dep3: IDep3 {}

                               interface IDep4 {}
                               class Dep4: IDep4 {}

                               interface IDep5 {}
                               class Dep5: IDep5 {}

                               interface IDep6 {}
                               class Dep6: IDep6 {}

                               interface IDep7 {}
                               class Dep7: IDep7 {}

                               interface IDep8 {}
                               class Dep8: IDep8 {}

                               interface IDep9 {}
                               class Dep9: IDep9 {}

                               interface IDep10 {}
                               class Dep10: IDep10 {}

                               class Service
                               {
                                   public Service(
                                       Func<IDep1> d1Fact, Func<IDep2> d2Fact, Func<IDep3> d3Fact,
                                       Func<IDep4> d4Fact, Func<IDep5> d5Fact, Func<IDep6> d6Fact,
                                       Func<IDep7> d7Fact, Func<IDep8> d8Fact, Func<IDep9> d9Fact,
                                       Func<IDep10> d10Fact)
                                   {
                                       var a1 = d1Fact();
                                       var a2 = d2Fact();
                                       var a3 = d3Fact();
                                       var a4 = d4Fact();
                                       var a5 = d5Fact();
                                       var a6 = d6Fact();
                                       var a7 = d7Fact();
                                       var a8 = d8Fact();
                                       var a9 = d9Fact();
                                       var a10 = d10Fact();

                                       var b1 = d1Fact();
                                       var b2 = d2Fact();
                                       var b3 = d3Fact();
                                       var b4 = d4Fact();
                                       var b5 = d5Fact();
                                       var b6 = d6Fact();
                                       var b7 = d7Fact();
                                       var b8 = d8Fact();
                                       var b9 = d9Fact();
                                       var b10 = d10Fact();

                                       var c1 = d1Fact();
                                       var c2 = d2Fact();
                                       var c3 = d3Fact();
                                       var c4 = d4Fact();
                                       var c5 = d5Fact();
                                       var c6 = d6Fact();
                                       var c7 = d7Fact();
                                       var c8 = d8Fact();
                                       var c9 = d9Fact();
                                       var c10 = d10Fact();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDep1>().To<Dep1>()
                                           .Bind<IDep2>().To<Dep2>()
                                           .Bind<IDep3>().To<Dep3>()
                                           .Bind<IDep4>().To<Dep4>()
                                           .Bind<IDep5>().To<Dep5>()
                                           .Bind<IDep6>().To<Dep6>()
                                           .Bind<IDep7>().To<Dep7>()
                                           .Bind<IDep8>().To<Dep8>()
                                           .Bind<IDep9>().To<Dep9>()
                                           .Bind<IDep10>().To<Dep10>()
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportPerBlockFuncWithCrossReferencedFactories()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep {}

                               class ServiceA
                               {
                                   public ServiceA(Func<IDep> depFact)
                                   {
                                       var d1 = depFact();
                                       var d2 = depFact();
                                       var d3 = depFact();
                                   }
                               }

                               class ServiceB
                               {
                                   public ServiceB(Func<IDep> depFact)
                                   {
                                       var d1 = depFact();
                                       var d2 = depFact();
                                       var d3 = depFact();
                                   }
                               }

                               class ServiceC
                               {
                                   public ServiceC(
                                       Func<ServiceA> aFact,
                                       Func<ServiceB> bFact,
                                       Func<IDep> depFact)
                                   {
                                       var a1 = aFact();
                                       var b1 = bFact();
                                       var c1 = depFact();
                                       var a2 = aFact();
                                       var b2 = bFact();
                                       var c2 = depFact();
                                   }
                               }

                               class ServiceD
                               {
                                   public ServiceD(
                                       Func<ServiceA> aFact,
                                       Func<ServiceB> bFact,
                                       Func<ServiceC> cFact,
                                       Func<IDep> depFact)
                                   {
                                       var a1 = aFact();
                                       var b1 = bFact();
                                       var c1 = cFact();
                                       var d1 = depFact();
                                       var a2 = aFact();
                                       var b2 = bFact();
                                       var c2 = cFact();
                                       var d2 = depFact();
                                   }
                               }

                               class ServiceE
                               {
                                   public ServiceE(
                                       Func<ServiceA> aFact,
                                       Func<ServiceB> bFact,
                                       Func<ServiceC> cFact,
                                       Func<ServiceD> dFact,
                                       Func<IDep> depFact)
                                   {
                                       var a1 = aFact();
                                       var b1 = bFact();
                                       var c1 = cFact();
                                       var d1 = dFact();
                                       var e1 = depFact();
                                       var a2 = aFact();
                                       var b2 = bFact();
                                       var c2 = cFact();
                                       var d2 = dFact();
                                       var e2 = depFact();
                                       var a3 = aFact();
                                       var b3 = bFact();
                                       var c3 = cFact();
                                       var d3 = dFact();
                                       var e3 = depFact();
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode=On
                                       DI.Setup("Composition")
                                          .Bind<Func<TT>>()
                                               .As(Lifetime.PerBlock)
                                               .To(ctx => new Func<TT>(() =>
                                               {
                                                   ctx.Inject<TT>(ctx.Tag, out var value);
                                                   return value;
                                               }))
                                           .Bind<IDep>().To<Dep>()
                                           .Bind<ServiceA>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDep>>(out var depFact);
                                                   return new ServiceA(depFact);
                                               })
                                           .Bind<ServiceB>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<IDep>>(out var depFact);
                                                   return new ServiceB(depFact);
                                               })
                                           .Bind<ServiceC>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<ServiceA>>(out var aFact);
                                                   ctx.Inject<Func<ServiceB>>(out var bFact);
                                                   ctx.Inject<Func<IDep>>(out var depFact);
                                                   return new ServiceC(aFact, bFact, depFact);
                                               })
                                           .Bind<ServiceD>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<ServiceA>>(out var aFact);
                                                   ctx.Inject<Func<ServiceB>>(out var bFact);
                                                   ctx.Inject<Func<ServiceC>>(out var cFact);
                                                   ctx.Inject<Func<IDep>>(out var depFact);
                                                   return new ServiceD(aFact, bFact, cFact, depFact);
                                               })
                                           .Bind<ServiceE>().To(ctx =>
                                               {
                                                   ctx.Inject<Func<ServiceA>>(out var aFact);
                                                   ctx.Inject<Func<ServiceB>>(out var bFact);
                                                   ctx.Inject<Func<ServiceC>>(out var cFact);
                                                   ctx.Inject<Func<ServiceD>>(out var dFact);
                                                   ctx.Inject<Func<IDep>>(out var depFact);
                                                   return new ServiceE(aFact, bFact, cFact, dFact, depFact);
                                               })
                                           .Root<ServiceE>("Service");
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
    }

    [Fact]
    public async Task ShouldSupportDefaultLifetimeWithTypeWhenAutoBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Service {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DefaultLifetime<Service>(Lifetime.Singleton)
                                           .Root<Service>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldIgnoreDefaultLifetimeWithoutTypeWhenAutoBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Service {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DefaultLifetime(Lifetime.Singleton)
                                           .Root<Service>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["False"], result);
    }

    [Fact]
    public async Task ShouldSupportDefaultLifetimeWithMultipleTypesWhenAutoBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Service1 {}
                               class Service2 {}
                               class Service3 {}
                               class Service4 {}
                               class Service5 {}
                               
                               static class Setup
                               {
                                private static void SetupComposition()
                                {
                                 DI.Setup(nameof(Composition))
                                  .DefaultLifetime<Service1>(Lifetime.Singleton)
                                  .DefaultLifetime<Service2>(Lifetime.Singleton)
                                  .DefaultLifetime<Service3>(Lifetime.Singleton)
                                  .DefaultLifetime<Service4>(Lifetime.Singleton)
                                  .DefaultLifetime<Service5>(Lifetime.Singleton)
                                  .Root<Service1>("Service1")
                                  .Root<Service2>("Service2")
                                  .Root<Service3>("Service3")
                                  .Root<Service4>("Service4")
                                  .Root<Service5>("Service5");
                                }
                               }

                               public class Program
                               {
                                public static void Main()
                                {
                                 var composition = new Composition();
                                 Console.WriteLine(composition.Service1 == composition.Service1);
                                 Console.WriteLine(composition.Service2 == composition.Service2);
                                 Console.WriteLine(composition.Service3 == composition.Service3);
                                 Console.WriteLine(composition.Service4 == composition.Service4);
                                 Console.WriteLine(composition.Service5 == composition.Service5);
                                }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportDefaultLifetimeWithDeepHierarchyWhenAutoBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency : IDependency {}

                               interface IServiceA
                               {
                                IDependency Dep { get; }
                               }
                               
                               class ServiceA : IServiceA
                               {
                                public ServiceA(IDependency dep) => Dep = dep;
                                public IDependency Dep { get; }
                               }

                               interface IServiceB
                               {
                                ServiceA ServiceA { get; }
                                IDependency Dep { get; }
                               }
                               
                               class ServiceB : IServiceB
                               {
                                public ServiceB(ServiceA serviceA, IDependency dep)
                                {
                                 ServiceA = serviceA;
                                 Dep = dep;
                                }
                                public ServiceA ServiceA { get; }
                                public IDependency Dep { get; }
                               }

                               interface IServiceC
                               {
                                ServiceB ServiceB { get; }
                                IDependency Dep { get; }
                               }
                               
                               class ServiceC : IServiceC
                               {
                                public ServiceC(ServiceB serviceB, IDependency dep)
                                {
                                 ServiceB = serviceB;
                                 Dep = dep;
                                }
                                public ServiceB ServiceB { get; }
                                public IDependency Dep { get; }
                               }
                               
                               static class Setup
                               {
                                private static void SetupComposition()
                                {
                                 DI.Setup(nameof(Composition))
                                  .Bind().To<Dependency>()
                                  .DefaultLifetime<ServiceA>(Lifetime.Singleton)
                                  .DefaultLifetime<ServiceB>(Lifetime.Singleton)
                                  .DefaultLifetime<ServiceC>(Lifetime.Singleton)
                                  .Root<ServiceC>("Root");
                                }
                               }

                               public class Program
                               {
                                public static void Main()
                                {
                                 var composition = new Composition();
                                 var root1 = composition.Root;
                                 var root2 = composition.Root;
                                 Console.WriteLine(root1 == root2);
                                 Console.WriteLine(root1.ServiceB == root2.ServiceB);
                                 Console.WriteLine(root1.ServiceB.ServiceA == root2.ServiceB.ServiceA);
                                 Console.WriteLine(root1.Dep == root2.Dep);
                                 Console.WriteLine(root1.ServiceB.Dep != root1.Dep);
                                }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportDefaultLifetimeWithMixedLifetimesWhenAutoBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class SingletonService {}

                               class ScopedService {}

                               class TransientService {}

                               class RootService
                               {
                                public RootService(
                                 SingletonService singleton,
                                 ScopedService scoped,
                                 TransientService transient)
                                {
                                 _singleton = singleton;
                                 _scoped = scoped;
                                 _transient = transient;
                                }

                                private readonly SingletonService _singleton;
                                private readonly ScopedService _scoped;
                                private readonly TransientService _transient;

                                public SingletonService Singleton => _singleton;
                                public ScopedService Scoped => _scoped;
                                public TransientService Transient => _transient;
                               }
                               
                               static class Setup
                               {
                                private static void SetupComposition()
                                {
                                 DI.Setup(nameof(Composition))
                                  .DefaultLifetime<SingletonService>(Lifetime.Singleton)
                                  .DefaultLifetime<ScopedService>(Lifetime.Scoped)
                                  .DefaultLifetime(Lifetime.Singleton)
                                  .Root<RootService>("Root");
                                }
                               }

                               public class Program
                               {
                                public static void Main()
                                {
                                 var composition = new Composition();
                                 var root1 = composition.Root;
                                 var root2 = composition.Root;
                                 Console.WriteLine(root1.Singleton == root2.Singleton);
                                 Console.WriteLine(root1.Scoped == root2.Scoped);
                                 Console.WriteLine(root1.Transient != root2.Transient);
                                }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }
}