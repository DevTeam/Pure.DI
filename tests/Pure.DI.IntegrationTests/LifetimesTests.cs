﻿namespace Pure.DI.IntegrationTests;

using Core;

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
    public async Task ShouldDefaultLifetimeForTypIsNotMatch()
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
    public async Task ShouldDefaultLifetimeForTypIsNotMatchButHasDefault()
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
    public async Task ShouldSupportPreResolveWithinFunc()
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
    public async Task ShouldSupportPreResolveWithinSingleton()
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

    [Theory]
    [InlineData(Lifetime.PerBlock, Names.PerBlockVariablePrefix)]
    [InlineData(Lifetime.PerResolve, Names.PerResolveVariablePrefix)]
    internal async Task ShouldSupportLifetimeOptimizationWhenComplex(Lifetime lifetime, string prefix)
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
                           
                               interface IDependency2 { }
                           
                               class Dependency2: IDependency2 { }
                           
                               interface IService
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                               }
                           
                               class Service: IService
                               {
                                   public Service(IDependency1 dep1, IDependency2 dep2)
                                   {
                                       Dep1 = dep1;
                                       Dep2 = dep2;
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                           
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency1>().As(Lifetime.#lifetime#).To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
                                           .Bind<IService>().To<Service>()
                                           .Bind<IService>(2).To<Service>()
                                           .Root<IService>("Service")
                                           .Root<IService>("Service2", 2);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       var service2 = composition.Service2;
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains(prefix).ShouldBeFalse(result);
    }

    [Theory]
    [InlineData(Lifetime.PerBlock, Names.PerBlockVariablePrefix)]
    [InlineData(Lifetime.PerResolve, Names.PerResolveVariablePrefix)]
    internal async Task ShouldSupportLifetimeOptimization(Lifetime lifetime, string prefix)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency { }
                           
                               class Dependency : IDependency {}
                               
                               interface IService { }
                               
                               class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               class Consumer
                               {
                                   private readonly IService _service;
                                   
                                   public Consumer(IService service)
                                   {
                                       _service = service;
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.#lifetime#).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<Consumer>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var consumer = composition.Root;
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains(prefix).ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldSupportLifetimeOptimizationWhenPerBlockAndLazy()
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
                           
                               interface IDependency2 { }
                           
                               class Dependency2: IDependency2 { }
                           
                               interface IService
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                               }
                           
                               class Service: IService
                               {
                                   public Service(Func<IDependency1> dep1, IDependency2 dep2)
                                   {
                                       Dep1 = dep1();
                                       Dep2 = dep2;
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                           
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency1>().As(Lifetime.PerBlock).To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
                                           .Bind<IService>().To<Service>()
                                           .Bind<IService>(2).To<Service>()
                                           .Root<Func<Lazy<IService>>>("Service")
                                           .Root<IService>("Service2", 2);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service().Value;
                                       var service2 = composition.Service2;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains(Names.PerBlockVariablePrefix).ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldNotSupportLifetimeOptimizationWhenPerResolveAndLazy()
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
                           
                               interface IDependency2 { }
                           
                               class Dependency2: IDependency2 { }
                           
                               interface IService
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                               }
                           
                               class Service: IService
                               {
                                   public Service(Func<IDependency1> dep1, IDependency2 dep2)
                                   {
                                       Dep1 = dep1();
                                       Dep2 = dep2;
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                           
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency1>().As(Lifetime.PerResolve).To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
                                           .Bind<IService>().To<Service>()
                                           .Bind<IService>(2).To<Service>()
                                           .Root<Lazy<IService>>("Service")
                                           .Root<IService>("Service2", 2);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service.Value;
                                       var service2 = composition.Service2;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains(Names.PerResolveVariablePrefix).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldNotSupportLifetimeOptimizationWhenPerResolveAndSeveralDeps()
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
                           
                               interface IDependency2 { }
                           
                               class Dependency2: IDependency2 { }
                           
                               interface IService
                               {
                                   IDependency1 Dep1 { get; }
                           
                                   IDependency2 Dep2 { get; }
                               }
                           
                               class Service: IService
                               {
                                   public Service(IDependency1 dep11, IDependency1 dep12, IDependency2 dep2)
                                   {
                                       Dep1 = dep11;
                                       Dep2 = dep2;
                                   }
                           
                                   public IDependency1 Dep1 { get; }
                           
                                   public IDependency2 Dep2 { get; }
                           
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency1>().As(Lifetime.PerResolve).To<Dependency1>()
                                           .Bind<IDependency2>().To<Dependency2>()
                                           .Bind<IService>().To<Service>()
                                           .Bind<IService>(2).To<Service>()
                                           .Root<IService>("Service")
                                           .Root<IService>("Service2", 2);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       var service2 = composition.Service2;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains(Names.PerResolveVariablePrefix).ShouldBeTrue(result);
    }
}