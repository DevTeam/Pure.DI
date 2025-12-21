namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the auto-binding feature, where the library automatically tries to resolve types that are not explicitly bound.
/// </summary>
public class AutoBindingTests
{
    [Fact]
    public async Task ShouldSupportAutoBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency
                               {
                                   public Dependency()
                                   {
                                       Console.WriteLine("Ctor1");
                                   }
                               }
                           
                               interface IService {}
                               class Service: IService
                               {
                                   public Service(Dependency dep)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().As(Lifetime.Transient).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = new Composition().Service;         
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ctor1"], result);
    }

    [Fact]
    public async Task ShouldSupportAutoBindingWhenSeveralConstructors()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency
                               {
                                   internal Dependency(int id)
                                   {
                                       Console.WriteLine("Ctor2");
                                   }
                           
                                   public Dependency()
                                   {
                                       Console.WriteLine("Ctor1");
                                   }
                               }
                           
                               interface IService {}
                               class Service: IService
                               {
                                   public Service(Dependency dep)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().As(Lifetime.Transient).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = new Composition().Service;         
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ctor1"], result);
    }

    [Fact]
    public async Task ShouldSupportNestedAutoBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency2
                               {
                                   public Dependency2()
                                   {
                                       Console.WriteLine("CtorDep2");
                                   }
                               }

                               class Dependency1
                               {
                                   public Dependency1(Dependency2 dep2)
                                   {
                                       Console.WriteLine("CtorDep1");
                                   }
                               }
                           
                               interface IService {}
                               class Service: IService
                               {
                                   public Service(Dependency1 dep1)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().As(Lifetime.Transient).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = new Composition().Service;         
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["CtorDep2", "CtorDep1"], result);
    }

    [Fact]
    public async Task ShouldSupportAutoBindingForRecord()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public record Dependency(string Name = "Default");
                           
                               interface IService {}
                               class Service: IService
                               {
                                   public Service(Dependency dep)
                                   {
                                       Console.WriteLine(dep.Name);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().As(Lifetime.Transient).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = new Composition().Service;         
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp12 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Default"], result);
    }

    [Fact]
    public async Task ShouldSupportAutoBindingForClassWithPrimaryConstructor()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public class Dependency(int id = 10)
                               {
                                   public int Id => id;
                               }
                           
                               interface IService {}
                               class Service: IService
                               {
                                   public Service(Dependency dep)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().As(Lifetime.Transient).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = new Composition().Service;         
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp12 });

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldNotSupportAutoBindingForInterface()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               interface IService {}
                               class Service: IService
                               {
                                   public Service(IDependency dep)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().As(Lifetime.Transient).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ShouldSupportAutoBindingForGenericType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency<T>
                               {
                                   public Dependency()
                                   {
                                       Console.WriteLine(typeof(T).Name);
                                   }
                               }
                           
                               interface IService {}
                               class Service: IService
                               {
                                   public Service(Dependency<int> dep)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().As(Lifetime.Transient).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = new Composition().Service;         
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32"], result);
    }

    [Fact]
    public async Task ShouldSupportAutoBindingWhenConstructorOrdinalAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency1 {}
                               class Dependency2 {}
                               class Dependency
                               {
                                   [Ordinal(1)]
                                   public Dependency(Dependency1 d1)
                                   {
                                       Console.WriteLine("Ctor1");
                                   }

                                   [Ordinal(0)]
                                   public Dependency(Dependency2 d2)
                                   {
                                       Console.WriteLine("Ctor2");
                                   }
                               }
                           
                               interface IService {}
                               class Service: IService
                               {
                                   public Service(Dependency dep)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().As(Lifetime.Transient).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = new Composition().Service;         
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ctor2"], result);
    }
    [Fact]
    public async Task ShouldSupportAutoBindingForValueType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               struct Dependency
                               {
                                   public Dependency() => Console.WriteLine("Struct ctor");
                               }
                           
                               interface IService {}
                               class Service: IService
                               {
                                   public Service(Dependency dep) {}
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
                                       var service = new Composition().Service;         
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Struct ctor"], result);
    }

    [Fact]
    public async Task ShouldSupportAutoBindingForNestedClass()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Outer
                               {
                                   public class Inner
                                   {
                                       public Inner() => Console.WriteLine("Inner ctor");
                                   }
                               }
                           
                               interface IService {}
                               class Service: IService
                               {
                                   public Service(Outer.Inner dep) {}
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
                                       var service = new Composition().Service;         
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Inner ctor"], result);
    }
}