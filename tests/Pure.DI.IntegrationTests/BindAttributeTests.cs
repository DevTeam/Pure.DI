namespace Pure.DI.IntegrationTests;

public class BindAttributeTests
{
    [Fact]
    public async Task ShouldSupportBindAttribute()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind]
                                   public IDependency Dep => new Dependency();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenField()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind]
                                   public IDependency Dep = new Dependency();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenStaticField()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind]
                                   public static IDependency Dep = new Dependency();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenStaticProperty()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind]
                                   public static IDependency Dep => new Dependency();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenTagInBindingSetup()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency, [Tag(33)] BaseComposition composition)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   public BaseComposition()
                                   {
                                       Console.WriteLine("created");
                                   }
                           
                                   [Bind]
                                   public IDependency Dep => new Dependency();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(33).As(Lifetime.Singleton).To<BaseComposition>()
                                           .Bind().To<Service>()
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
        result.StdOut.ShouldBe(["created"], result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenTypeIsDefined()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind(typeof(IDependency))]
                                   public Dependency Dep => new Dependency();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenTypeAndTagAreDefined()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind(typeof(IDependency), Lifetime.Singleton, null, 1, "abc")]
                                   public Dependency Dep => new Dependency();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenMethod()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind(typeof(IDependency), Lifetime.Singleton, null, 1, "abc")]
                                   public Dependency GetDep() => new Dependency();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenStaticMethod()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind(typeof(IDependency), Lifetime.Singleton, null, 1, "abc")]
                                   public static Dependency GetDep() => new Dependency();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenMethodWithArgs()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind(typeof(IDependency), Lifetime.Singleton, null, 1, "abc")]
                                   public Dependency GetDep(int intValue, string str)
                                   {
                                       Console.WriteLine(intValue);
                                       Console.WriteLine(str);
                                       return new Dependency();
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To(_ => 123)
                                           .Bind().To(_ => "Abc")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
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
        result.StdOut.ShouldBe(["123", "Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenGenericMethodResultWithArgs()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency<int> dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind(typeof(IDependency<int>), Lifetime.Singleton, null, 1, "abc")]
                                   public Dependency<int> GetDep(int intValue, string str)
                                   {
                                       Console.WriteLine(intValue);
                                       Console.WriteLine(str);
                                       return new Dependency<int>();
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To(_ => 123)
                                           .Bind().To(_ => "Abc")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
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
        result.StdOut.ShouldBe(["123", "Abc"], result);
    }

    [Theory]
    [InlineData("Pure.DI.", "Bind")]
    [InlineData("", "Bind")]
    [InlineData("global::Pure.DI.", "Bind")]
    [InlineData("Pure.DI.", "BindAttribute")]
    [InlineData("", "BindAttribute")]
    [InlineData("global::Pure.DI.", "BindAttribute")]
    public async Task ShouldSupportBindAttributeWhenGenericMethodWithArgs(string typeName, string attrName)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency<T> { }
                           
                               internal class Dependency<T> : IDependency<T>
                               {
                                   public Dependency() { }
                               }
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency<int> dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [#TypeName#AttrName(typeof(Sample.IDependency<#TypeNameTT>), #TypeNameLifetime.Transient, null)]
                                   public Sample.IDependency<T> GetDep<T>(int id)
                                   {
                                       Console.WriteLine(id);
                                       return new Dependency<T>();
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To(_ => 77)
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
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
                           """.Replace("#TypeName", typeName).Replace("#AttrName", attrName).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["77"], result);
    }

    [Theory]
    [InlineData("Pure.DI.")]
    [InlineData("")]
    [InlineData("global::Pure.DI.")]
    public async Task ShouldSupportBindAttributeWhenGenericMethodWithGenericArgs(string typeName)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency<T> { }
                           
                               internal class Dependency<T> : IDependency<T>
                               {
                                   public Dependency(T val) { }
                               }
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency<int> dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [#TypeNameBind(typeof(Sample.IDependency<#TypeNameTT>), #TypeNameLifetime.Singleton, null, 1, "abc")]
                                   public Sample.IDependency<T> GetDep<T>(T val, string str)
                                   {
                                       Console.WriteLine(val);
                                       Console.WriteLine(str);
                                       return new Dependency<T>(val);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To(_ => 123)
                                           .Bind().To(_ => "Abc")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
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
                           """.Replace("#TypeName", typeName).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["123", "Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenGenericMethod()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public interface IMyDependency
                               {
                                   public void DoSomething();
                               }
                               
                               internal class MyDependency : IMyDependency
                               {
                                   public void DoSomething()
                                   {
                                   }
                               }
                           
                               public interface IMyGenericService<T>
                               {
                                   public void DoSomething(T value);
                               }
                               
                               internal class MyGenericService<T> : IMyGenericService<T>
                               {
                                   private readonly IMyDependency _dependency;
                           
                                   public MyGenericService(IMyDependency dependency)
                                   {
                                       _dependency = dependency;
                                   }
                           
                                   public void DoSomething(T value)
                                   {
                                       Console.WriteLine(value); 
                                       _dependency.DoSomething();
                                   }
                               }
                           
                               interface IDependency {};
                           
                               class Dependency : IDependency {};
                           
                               interface IService {};
                           
                               class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                           
                               class App
                               {
                                   private readonly IMyGenericService<int> _myService;
                           
                                   public App(IService service, IMyGenericService<int> myService)
                                   {
                                       _myService = myService;
                                       Service = service;
                                   }
                           
                                   public IService Service { get; }
                           
                                   public void DoSomething(int value) => _myService.DoSomething(value);
                               }
                               
                               internal class BaseComposition
                               {
                                   [global::Pure.DI.BindAttribute(typeof(Sample.IMyGenericService<Pure.DI.TT>), Pure.DI.Lifetime.Transient, null)]
                                   public Sample.IMyGenericService<T> MyService<T>()
                                   {
                                       return new Sample.MyGenericService<T>(new Sample.MyDependency());
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           // Binds to exposed composition roots from other project
                                           .Bind().As(Lifetime.Singleton).To<BaseComposition>()
                                           .Root<App>("App");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var app = composition.App;
                                       app.DoSomething(99);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["99"], result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeWhenTypeIsDefinedAndTagIsNull()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               internal class BaseComposition
                               {
                                   [Bind(typeof(IDependency), Lifetime.Singleton, null)]
                                   public Dependency Dep => new Dependency();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<BaseComposition>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportExposedRootKind()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service", kind: RootKinds.Exposed);
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportExposedRootKindWithTags()
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
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind(1, null, "abc").To<Service>()
                                           .Root<IService>("Service", 1, kind: RootKinds.Exposed);
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
}

/*
 DI.Setup("SourceBaseComposition")
.Bind().To<Dependency>()
.Root<IDependency>("Dep", kind: RootKinds.Exposed);
 */