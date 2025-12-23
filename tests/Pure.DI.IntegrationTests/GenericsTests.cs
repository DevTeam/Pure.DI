namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the generic type bindings and their resolution.
/// </summary>
public class GenericsTests
{
    [Fact]
    public async Task ShouldSupportCustomGenericTypeArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface TTMy { };
                               
                               internal interface IDependency<T> { }
                           
                               internal class Dependency<T> : IDependency<T> { }
                           
                               internal interface IService
                               {
                                   IDependency<int> IntDependency { get; }
                                   
                                   IDependency<string> StringDependency { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency<int> intDependency, IDependency<string> stringDependency)
                                   {
                                       IntDependency = intDependency;
                                       StringDependency = stringDependency;
                                   }
                                   
                                   public IDependency<int> IntDependency { get; }
                                   
                                   public IDependency<string> StringDependency { get; }
                               }    
                           
                               public class Program
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .GenericTypeArgument<TTMy>()
                                           .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;    
                                       Console.WriteLine(service.IntDependency.GetType());
                                       Console.WriteLine(service.StringDependency.GetType());     
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency`1[System.Int32]", "Sample.Dependency`1[System.String]"], result);
    }

    [Fact]
    public async Task ShouldSupportCustomGenericTypeArgumentAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
                               internal sealed class MyGenericAttribute : global::System.Attribute { }
                           
                               [MyGenericAttribute]
                               internal interface TTMy { };
                               
                               internal interface IDependency<T> { }
                           
                               internal class Dependency<T> : IDependency<T> { }
                           
                               internal interface IService
                               {
                                   IDependency<int> IntDependency { get; }
                                   
                                   IDependency<string> StringDependency { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency<int> intDependency, IDependency<string> stringDependency)
                                   {
                                       IntDependency = intDependency;
                                       StringDependency = stringDependency;
                                   }
                                   
                                   public IDependency<int> IntDependency { get; }
                                   
                                   public IDependency<string> StringDependency { get; }
                               }    
                           
                               public class Program
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .GenericTypeArgumentAttribute<MyGenericAttribute>()
                                           .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;    
                                       Console.WriteLine(service.IntDependency.GetType());
                                       Console.WriteLine(service.StringDependency.GetType());     
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency`1[System.Int32]", "Sample.Dependency`1[System.String]"], result);
    }
    [Fact]
    public async Task ShouldSupportGenerics()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency<T> { }
                           
                               internal class Dependency<T> : IDependency<T> { }
                           
                               internal interface IService
                               {
                                   IDependency<int> IntDependency { get; }
                                   
                                   IDependency<string> StringDependency { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency<int> intDependency, IDependency<string> stringDependency)
                                   {
                                       IntDependency = intDependency;
                                       StringDependency = stringDependency;
                                   }
                                   
                                   public IDependency<int> IntDependency { get; }
                                   
                                   public IDependency<string> StringDependency { get; }
                               }    
                           
                               public class Program
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;    
                                       Console.WriteLine(service.IntDependency.GetType());
                                       Console.WriteLine(service.StringDependency.GetType());     
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency`1[System.Int32]", "Sample.Dependency`1[System.String]"], result);
    }
    [Fact]
    public async Task ShouldSupportMultipleGenericMarkers()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T1, T2> {}
                               class Dependency<T1, T2> : IDependency<T1, T2> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT1, TT2>>().To<Dependency<TT1, TT2>>()
                                           .Root<IDependency<int, string>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`2"], result);
    }

    [Fact]
    public async Task ShouldSupportNestedGenerics()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<T> {}
                               class Box<T> : IBox<T> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IBox<TT>>().To<Box<TT>>()
                                           .Root<IBox<IBox<int>>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Box`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericConstraints()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> where T : class {}
                               class Dependency<T> : IDependency<T> where T : class {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Root<IDependency<string>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericStructConstraint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> where T : struct {}
                               class Dependency<T> : IDependency<T> where T : struct {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TTS>>().To<Dependency<TTS>>()
                                           .Root<IDependency<int>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericNewConstraint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> where T : new() {}
                               class Dependency<T> : IDependency<T> where T : new() {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TTN>>().To<Dependency<TTN>>()
                                           .Root<IDependency<object>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericBaseClassConstraint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Base {}
                               class Derived : Base {}
                               class TTBase: Base { }
                               interface IDependency<T> where T : Base {}
                               class Dependency<T> : IDependency<T> where T : Base {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .GenericTypeArgument<TTBase>()
                                           .Bind<IDependency<TTBase>>().To<Dependency<TTBase>>()
                                           .Root<IDependency<Derived>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericInterfaceConstraint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBase {}
                               class Derived : IBase {}
                               interface TTInterface: IBase { }
                               interface IDependency<T> where T : IBase {}
                               class Dependency<T> : IDependency<T> where T : IBase {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .GenericTypeArgument<TTInterface>()
                                           .Bind<IDependency<TTInterface>>().To<Dependency<TTInterface>>()
                                           .Root<IDependency<Derived>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportThreeGenericArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T1, T2, T3> {}
                               class Dependency<T1, T2, T3> : IDependency<T1, T2, T3> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT1, TT2, TT3>>().To<Dependency<TT1, TT2, TT3>>()
                                           .Root<IDependency<int, string, double>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`3"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericsInComplexComposition()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T> : IDependency<T> {}
                               interface IService { IDependency<int> Dep { get; } }
                               class Service : IService 
                               {
                                   public Service(IDependency<int> dep) => Dep = dep;
                                   public IDependency<int> Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.Dep.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericArray()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T> : IDependency<T> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Root<IDependency<int>[]>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["IDependency`1[]"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithMixedArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T1, T2> {}
                               class Dependency<T1, T2> : IDependency<T1, T2> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT1, int>>().To<Dependency<TT1, int>>()
                                           .Root<IDependency<string, int>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`2"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithNestedGenericArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T> : IDependency<T> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Root<IDependency<List<int>>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportMultipleGenericBindings()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency1<T> : IDependency<T> {}
                               class Dependency2<T> : IDependency<T> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>(1).To<Dependency1<TT>>()
                                           .Bind<IDependency<TT>>(2).To<Dependency2<TT>>()
                                           .Root<IDependency<int>>("Root1", 1)
                                           .Root<IDependency<int>>("Root2", 2);
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root1.GetType().Name);
                                       Console.WriteLine(composition.Root2.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency1`1", "Dependency2`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithRefTypeConstraint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> where T : class {}
                               class Dependency<T> : IDependency<T> where T : class {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Root<IDependency<string>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithMultipleConstraints()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBase {}
                               class TTMy: IBase { public TTMy() {} }
                               interface IDependency<T> where T : class, IBase, new() {}
                               class Dependency<T> : IDependency<T> where T : class, IBase, new() {}
                               class MyType : IBase { public MyType() {} }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .GenericTypeArgument<TTMy>()
                                           .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>()
                                           .Root<IDependency<MyType>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithImplicitTypeArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T> : IDependency<T> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Root<IDependency<int>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithExplicitTypeArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T> : IDependency<T> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<int>>().To<Dependency<int>>()
                                           .Root<IDependency<int>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithNestedGenericTypes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<T> {}
                               class Box<T> : IBox<T> {}
                               interface IDependency<T> {}
                               class Dependency<T> : IDependency<T> 
                               {
                                   public Dependency(T value) {}
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IBox<TT1>>().To<Box<TT1>>()
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Root<IDependency<IBox<int>>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithDifferentArity()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               interface IDependency<T1, T2> {}
                               class Dependency<T> : IDependency<T> {}
                               class Dependency<T1, T2> : IDependency<T1, T2> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Bind<IDependency<TT1, TT2>>().To<Dependency<TT1, TT2>>()
                                           .Root<IDependency<int>>("Root1")
                                           .Root<IDependency<int, string>>("Root2");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root1.GetType().Name);
                                       Console.WriteLine(composition.Root2.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dependency`1", "Dependency`2"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> { T Value { get; } }
                               class Dependency<T> : IDependency<T> 
                               {
                                   public Dependency(T value) => Value = value;
                                   public T Value { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To(ctx => {
                                               ctx.Inject<TT>(out var val);
                                               return new Dependency<TT>(val);
                                           })
                                           .Bind<int>().To(_ => 10)
                                           .Root<IDependency<int>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.Value);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["10"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithNamedBindings()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T> : IDependency<T> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>("A").To<Dependency<TT>>()
                                           .Bind<IDependency<TT>>("B").To<Dependency<TT>>()
                                           .Root<IDependency<int>>("RootA", "A")
                                           .Root<IDependency<int>>("RootB", "B");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.RootA != null);
                                       Console.WriteLine(composition.RootB != null);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericWithSingletonLifetime()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T> : IDependency<T> {}
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().As(Lifetime.Singleton).To<Dependency<TT>>()
                                           .Root<IDependency<int>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }
}