namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the [RootBind] attribute.
/// </summary>
public class RootBindTests
{
    [Fact]
    public async Task ShouldSupportRootBind()
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
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency>().To<Dependency>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<IDependency>();
                                       Console.WriteLine(service.GetType() == typeof(Dependency));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithRootKind()
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
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency>("Root", RootKinds.Method).To<Dependency>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root();
                                       Console.WriteLine(service.GetType() == typeof(Dependency));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithRootName()
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
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency>("Root").To<Dependency>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.GetType() == typeof(Dependency));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithRootNameTemplate()
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
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency>("Root{type}").To<Dependency>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.RootIDependency;
                                       Console.WriteLine(service.GetType() == typeof(Dependency));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithRootNameTemplateWithTag()
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
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency>("Root{type}_{tag}", RootKinds.Property, "Xyz", 99).To<Dependency>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.RootIDependency_Xyz;
                                       Console.WriteLine(service.GetType() == typeof(Dependency));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithTags()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .RootBind<IDependency>("Root", RootKinds.Property, "RootTag", "Dep2").As(Singleton).To<Dependency>()
                                           .Root<IDependency>("Root2", "Dep2")
                                           .Root<IDependency>("Root3");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root == composition.Root2);
                                       Console.WriteLine(composition.Root != composition.Root3);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldSupportRootBindWithSingleton()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency: IDependency {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency>().As(Singleton).To<Dependency>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Resolve<IDependency>();
                                       var service2 = composition.Resolve<IDependency>();
                                       Console.WriteLine(service1 == service2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithPerResolve()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency: IDependency {}
                               
                               interface IService {
                                   IDependency Dep1 { get; }
                                   IDependency Dep2 { get; }
                               }
                               
                               class Service: IService {
                                   public Service(IDependency dep1, IDependency dep2) {
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
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(PerResolve).To<Dependency>()
                                           .RootBind<IService>().To<Service>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<IService>();
                                       Console.WriteLine(service.Dep1 == service.Dep2);
                                       
                                       var service2 = composition.Resolve<IService>();
                                       Console.WriteLine(service.Dep1 != service2.Dep1);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithGenericOpen()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T>: IDependency<T> {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency<TT>>("GetDependency").To<Dependency<TT>>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.GetDependency<int>();
                                       Console.WriteLine(service.GetType() == typeof(Dependency<int>));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithGenericClosed()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T>: IDependency<T> {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency<int>>().To<Dependency<int>>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<IDependency<int>>();
                                       Console.WriteLine(service.GetType() == typeof(Dependency<int>));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithIEnumerable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Linq;
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
                                           .Bind<IDependency>(1).To<Dependency1>()
                                           .Bind<IDependency>(2).To<Dependency2>()
                                           .Root<IEnumerable<IDependency>>("AllDependencies");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var services = composition.AllDependencies.ToList();
                                       Console.WriteLine(services.Count);
                                       Console.WriteLine(services[0] is Dependency1);
                                       Console.WriteLine(services[1] is Dependency2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithArray()
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

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<IDependency[]>("AllDependencies");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var services = composition.AllDependencies;
                                       Console.WriteLine(services.Length);
                                       Console.WriteLine(services[0] is Dependency);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency: IDependency {
                                   public Dependency(string name) => Name = name;
                                   public string Name { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency>().To(ctx => new Dependency("Created by factory"));
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = (Dependency)composition.Resolve<IDependency>();
                                       Console.WriteLine(service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Created by factory"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithCompositionArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency: IDependency {
                                   public Dependency(string name) => Name = name;
                                   public string Name { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Arg<string>("depName")
                                           .RootBind<IDependency>().To<Dependency>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Custom Name");
                                       var service = (Dependency)composition.Resolve<IDependency>();
                                       Console.WriteLine(service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Custom Name"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithNestedDependencies()
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
                               class Service: IService {
                                   public Service(IDependency dep) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .RootBind<IService>().To<Service>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<IService>();
                                       Console.WriteLine(service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithStruct()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               struct Dependency {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<Dependency>().To<Dependency>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<Dependency>();
                                       Console.WriteLine(service.GetType().IsValueType);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithRecord()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public record Dependency(string Name);

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>().To(ctx => "Record Name")
                                           .RootBind<Dependency>().To<Dependency>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<Dependency>();
                                       Console.WriteLine(service.Name);
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp9 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Record Name"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithFunc()
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

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<Func<IDependency>>("GetDependency");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.GetDependency;
                                       var service = factory();
                                       Console.WriteLine(service is Dependency);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithLazy()
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

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<Lazy<IDependency>>("LazyDependency");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var lazy = composition.LazyDependency;
                                       Console.WriteLine(lazy.Value is Dependency);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithIReadOnlyList()
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

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<IReadOnlyList<IDependency>>("AllDependencies");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var services = composition.AllDependencies;
                                       Console.WriteLine(services.Count);
                                       Console.WriteLine(services[0] is Dependency);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindInPartialSetup()
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

                               static class Setup
                               {
                                   private static void Setup1()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency>().To<Dependency>();
                                   }
                                   
                                   private static void Setup2()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.ToString, "On");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<IDependency>();
                                       Console.WriteLine(service is Dependency);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithInternalClass()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency {}
                               internal class Dependency: IDependency {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency>().To<Dependency>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<IDependency>();
                                       Console.WriteLine(service is Dependency);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithCustomRootKind()
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

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency>("MyRoot", RootKinds.Internal | RootKinds.Method).To<Dependency>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.MyRoot();
                                       Console.WriteLine(service is Dependency);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithWarningOnOverride()
    {
        // Given

        // When
        var result = await """
                           using System;
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
                                           .RootBind<IDependency>().To<Dependency1>()
                                           .Bind<IDependency>().To<Dependency2>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.ShouldBeEmpty(result);
        result.Warnings.Any(i => i.Id == "DIW000").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportRootBindForMultipleInterfaces()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency1 {}
                               interface IDependency2 {}
                               class Dependency: IDependency1, IDependency2 {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootBind<IDependency1>().To<Dependency>();
                                           
                                       DI.Setup("Composition")
                                           .RootBind<IDependency2>().To<Dependency>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Resolve<IDependency1>();
                                       var service2 = composition.Resolve<IDependency2>();
                                       Console.WriteLine(service1 is Dependency);
                                       Console.WriteLine(service2 is Dependency);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }
#endif
}