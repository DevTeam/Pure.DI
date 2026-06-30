namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the [Export] attribute, which allows defining bindings directly on members like fields, properties, or methods.
/// </summary>
public class ExportAttributeTests
{
    [Theory]
    [InlineData("Pure.DI.", "Export")]
    [InlineData("", "Export")]
    [InlineData("global::Pure.DI.", "Export")]
    [InlineData("Pure.DI.", "ExportAttribute")]
    [InlineData("", "ExportAttribute")]
    [InlineData("global::Pure.DI.", "ExportAttribute")]
    public async Task ShouldSupportExportAttributeWhenGenericMethodWithArgs(string typeName, string attrName)
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
    public async Task ShouldSupportExportAttributeWhenGenericMethodWithGenericArgs(string typeName)
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
                                   [#TypeNameExport(typeof(Sample.IDependency<#TypeNameTT>), #TypeNameLifetime.Singleton, null, 1, "abc")]
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
    public async Task ShouldSupportExportAttribute()
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
                                   [Export]
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

    [Theory]
    [InlineData(Lifetime.Singleton)]
    [InlineData(Lifetime.Transient)]
    [InlineData(Lifetime.PerBlock)]
    [InlineData(Lifetime.PerResolve)]
    internal async Task ShouldSupportExportAttributeWhenExportToCompositionType(Lifetime lifetime)
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
                               
                               partial class Composition
                               {
                                   [Export] public IDependency Dep => new Dependency();
                           
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().As(Lifetime.#lifetime#).To<Composition>()
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
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Theory]
    [InlineData(Lifetime.Singleton)]
    [InlineData(Lifetime.Transient)]
    [InlineData(Lifetime.PerBlock)]
    [InlineData(Lifetime.PerResolve)]
    internal async Task ShouldSupportExportAttributeWhenExportToCompositionInstance(Lifetime lifetime)
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
                               
                               partial class Composition
                               {
                                   [Export] public IDependency Dep => new Dependency();
                           
                                   private void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().As(Lifetime.#lifetime#).To(_ => this)
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
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportExportAttributeWhenSeveral()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency { }
                               
                               internal interface IDependency2 { }
                           
                               internal class Dependency : IDependency { }
                               
                               internal class Dependency2 : IDependency2 { }
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency, IDependency2 dependency2)
                                   {
                                   }
                               }
                               
                               internal class ServiceProvider
                               {
                                   [Export] public IDependency Dep => new Dependency();
                                   
                                   [Export] public IDependency2 Dep2 => new Dependency2();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<ServiceProvider>()
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
    public async Task ShouldSupportExportAttributeWhenField()
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
                                   [Export]
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
    public async Task ShouldSupportExportAttributeWhenGenericMethod()
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
                                   [global::Pure.DI.ExportAttribute(typeof(Sample.IMyGenericService<Pure.DI.TT>), Pure.DI.Lifetime.Transient, null)]
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
    public async Task ShouldSupportExportAttributeWhenGenericMethodResultWithArgs()
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
                                   [Export(typeof(IDependency<int>), Lifetime.Singleton, null, 1, "abc")]
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

    [Fact]
    public async Task ShouldSupportExportAttributeWhenMethod()
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
                                   [Export(typeof(IDependency), Lifetime.Singleton, null, 1, "abc")]
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
    public async Task ShouldSupportExportAttributeWhenMethodWithArgs()
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
                                   [Export(typeof(IDependency), Lifetime.Singleton, null, 1, "abc")]
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
    public async Task ShouldSupportExportAttributeWhenStaticField()
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
                                   [Export]
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
    public async Task ShouldSupportExportAttributeWhenStaticMethod()
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
                                   [Export(typeof(IDependency), Lifetime.Singleton, null, 1, "abc")]
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
    public async Task ShouldSupportExportAttributeWhenStaticProperty()
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
                                   [Export]
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
    public async Task ShouldSupportExportAttributeWhenTagInBindingSetup()
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
                           
                                   [Export]
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
    public async Task ShouldSupportExportAttributeWhenTypeAndTagAreDefined()
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
                                   [Export(typeof(IDependency), Lifetime.Singleton, null, 1, "abc")]
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
    public async Task ShouldSupportExportAttributeWhenTypeIsDefined()
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
                                   [Export(typeof(IDependency))]
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
    public async Task ShouldSupportExportAttributeWhenTypeIsDefinedAndTagIsNull()
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
                                   [Export(typeof(IDependency), Lifetime.Singleton, null)]
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
    public async Task ShouldSupportExportedRootKind()
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
                                           .Root<IService>("Service", kind: RootKinds.Exported);
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
    public async Task ShouldSupportExportedRootKindWithTags()
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
                                           .Root<IService>("Service", 1, kind: RootKinds.Exported);
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
    public async Task ShouldSupportExportAttributeOnPropertyWithSingleton()
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

                               class BaseComposition
                               {
                                   [Export(typeof(IDependency), Lifetime.Singleton)]
                                   public IDependency Dep => new Dependency();
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<BaseComposition>().To<BaseComposition>()
                                           .Root<IDependency>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root1 = composition.Root;
                                       var root2 = composition.Root;
                                       Console.WriteLine(ReferenceEquals(root1, root2));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportAttributeWithMultipleTags()
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

                               class BaseComposition
                               {
                                   [Export(typeof(IDependency), Lifetime.Transient, "a")]
                                   [Export(typeof(IDependency), Lifetime.Transient, "b")]
                                   public IDependency Dep => new Dependency();
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<BaseComposition>().To<BaseComposition>()
                                           .Root<IDependency>("RootA", "a")
                                           .Root<IDependency>("RootB", "b");
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
    public async Task ShouldSupportExportedRootFromOtherComposition()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;

                           public class MyDependency { }

                           public interface IMyService { void DoSomething(); }

                           public class MyService : IMyService
                           {
                               public MyDependency MyDependency { get; }
                               public MyService(MyDependency myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public partial class CompositionInOtherProject
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency>()
                                       .Bind().To<MyService>()
                                       .Root<IMyService>("MyService", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService MyService { get; }

                               public Program(IMyService myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWithGenericType()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;

                           public class MyDependency<T> { }

                           public interface IMyService<T> { void DoSomething(); }

                           public class MyService<T> : IMyService<T>
                           {
                               public MyDependency<T> MyDependency { get; }
                               public MyService(MyDependency<T> myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public partial class CompositionInOtherProject<T>
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency<T>>()
                                       .Bind().To<MyService<T>>()
                                       .Root<IMyService<T>>("MyService", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject<int>>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService<int> MyService { get; }

                               public Program(IMyService<int> myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWithGenericTypes()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;

                           public class MyDependency<T1, T2> { }

                           public interface IMyService<T1, T2> { void DoSomething(); }

                           public class MyService<T1, T2> : IMyService<T1, T2>
                           {
                               public MyDependency<T1, T2> MyDependency { get; }
                               public MyService(MyDependency<T1, T2> myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public partial class CompositionInOtherProject<T1, T2>
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency<T1, T2>>()
                                       .Bind().To<MyService<T1, T2>>()
                                       .Root<IMyService<T1, T2>>("MyService", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject<int, string>>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService<int, string> MyService { get; }

                               public Program(IMyService<int, string> myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWithNestedGenericTypes()
    {
        // Given

        // When
        var result = await """
                           using System.Collections.Generic;
                           using Pure.DI;

                           public class MyDependency<T1, T2, T3> { }

                           public interface IMyService<T1, T2, T3> { void DoSomething(); }

                           public class MyService<T1, T2, T3> : IMyService<T1, T2, T3>
                           {
                               public MyDependency<T1, T2, T3> MyDependency { get; }
                               public MyService(MyDependency<T1, T2, T3> myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public partial class CompositionInOtherProject<T1, T2, T3>
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency<T1, List<T2>, T3>>()
                                       .Bind().To<MyService<T1, List<T2>, T3>>()
                                       .Root<IMyService<T1, List<T2>, T3>>("MyService", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject<int, string, double>>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService<int, List<string>, double> MyService { get; }

                               public Program(IMyService<int, List<string>, double> myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWithDeeplyNestedGenericTypes()
    {
        // Given

        // When
        var result = await """
                           using System.Collections.Generic;
                           using Pure.DI;

                           public class MyDependency<T> { }

                           public interface IMyService<T> { void DoSomething(); }

                           public class MyService<T> : IMyService<T>
                           {
                               public MyDependency<T> MyDependency { get; }
                               public MyService(MyDependency<T> myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public partial class CompositionInOtherProject<T1, T2>
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency<List<List<T1>>> >()
                                       .Bind().To<MyService<List<List<T1>>>>()
                                       .Root<IMyService<List<List<T1>>>>("MyService", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject<int, string>>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService<List<List<int>>> MyService { get; }

                               public Program(IMyService<List<List<int>>> myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWithDeeplyNestedGenericTypesAndConstraints()
    {
        // Given

        // When
        var result = await """
                           using System.Collections.Generic;
                           using Pure.DI;

                           public class MyDependency<T> { }

                           public interface IMyService<T> where T : class, new()
                           {
                               MyDependency<T> MyDependency { get; }
                               void DoSomething();
                           }

                           public class MyService<T> : IMyService<T> where T : class, new()
                           {
                               public MyDependency<T> MyDependency { get; }
                               public MyService(MyDependency<T> myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public class MyModel { }

                           public partial class CompositionInOtherProject<T1> where T1 : class, new()
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency<List<List<T1>>>>()
                                       .Bind().To<MyService<List<List<T1>>>>()
                                       .Root<IMyService<List<List<T1>>>>("MyService", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject<MyModel>>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService<List<List<MyModel>>> MyService { get; }

                               public Program(IMyService<List<List<MyModel>>> myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWithDeeplyNestedGenericTypesAndStructConstraints()
    {
        // Given

        // When
        var result = await """
                           using System.Collections.Generic;
                           using Pure.DI;

                           public class MyDependency<T> { }

                           public interface IMyService<T> where T : struct
                           {
                               MyDependency<T> MyDependency { get; }
                               void DoSomething();
                           }

                           public class MyService<T> : IMyService<T> where T : struct
                           {
                               public MyDependency<T> MyDependency { get; }
                               public MyService(MyDependency<T> myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public struct MyModel { }

                           public partial class CompositionInOtherProject<T1> where T1 : struct
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency<KeyValuePair<T1, KeyValuePair<T1, T1>>>>()
                                       .Bind().To<MyService<KeyValuePair<T1, KeyValuePair<T1, T1>>>>()
                                       .Root<IMyService<KeyValuePair<T1, KeyValuePair<T1, T1>>>>("MyService", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject<MyModel>>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService<KeyValuePair<MyModel, KeyValuePair<MyModel, MyModel>>> MyService { get; }

                               public Program(IMyService<KeyValuePair<MyModel, KeyValuePair<MyModel, MyModel>>> myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWithDeeplyNestedGenericTypesAndClassConstraints()
    {
        // Given

        // When
        var result = await """
                           using System.Collections.Generic;
                           using Pure.DI;

                           public class MyDependency<T> { }

                           public interface IMyService<T> where T : class
                           {
                               MyDependency<T> MyDependency { get; }
                               void DoSomething();
                           }

                           public class MyService<T> : IMyService<T> where T : class
                           {
                               public MyDependency<T> MyDependency { get; }
                               public MyService(MyDependency<T> myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public class MyModel { }

                           public partial class CompositionInOtherProject<T1> where T1 : class
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency<List<List<T1>>>>()
                                       .Bind().To<MyService<List<List<T1>>>>()
                                       .Root<IMyService<List<List<T1>>>>("MyService", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject<MyModel>>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService<List<List<MyModel>>> MyService { get; }

                               public Program(IMyService<List<List<MyModel>>> myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWithDeeplyNestedGenericTypesAndDisposableConstraints()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           public class MyDependency<T> { }

                           public interface IMyService<T> where T : class, IDisposable
                           {
                               MyDependency<T> MyDependency { get; }
                               void DoSomething();
                           }

                           public class MyService<T> : IMyService<T> where T : class, IDisposable
                           {
                               public MyDependency<T> MyDependency { get; }
                               public MyService(MyDependency<T> myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public class DisposableBox<T> : IDisposable
                           {
                               public void Dispose() { }
                           }

                           public class MyModel : IDisposable
                           {
                               public void Dispose() { }
                           }

                           public partial class CompositionInOtherProject<T1> where T1 : class, IDisposable
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency<DisposableBox<List<List<T1>>>>>()
                                       .Bind().To<MyService<DisposableBox<List<List<T1>>>>>()
                                       .Root<IMyService<DisposableBox<List<List<T1>>>>>("MyService", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject<MyModel>>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService<DisposableBox<List<List<MyModel>>>> MyService { get; }

                               public Program(IMyService<DisposableBox<List<List<MyModel>>>> myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWithMultipleGenericRoots()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;

                           public class MyDependency<T1, T2> { }

                           public interface IMyService<T1, T2> { void DoSomething(); }

                           public class MyService<T1, T2> : IMyService<T1, T2>
                           {
                               public MyDependency<T1, T2> MyDependency { get; }
                               public MyService(MyDependency<T1, T2> myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public partial class CompositionInOtherProject<T1, T2>
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency<T1, T2>>()
                                       .Bind().To<MyService<T1, T2>>()
                                       .Bind().To<MyService<T2, T1>>()
                                       .Root<IMyService<T1, T2>>("MyService", kind: RootKinds.Exported)
                                       .Root<IMyService<T2, T1>>("MyServiceReversed", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject<int, string>>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService<int, string> MyService { get; }
                               public IMyService<string, int> MyServiceReversed { get; }

                               public Program(
                                   IMyService<int, string> myService,
                                   IMyService<string, int> myServiceReversed)
                               {
                                   MyService = myService;
                                   MyServiceReversed = myServiceReversed;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                                   System.Console.WriteLine(program.MyServiceReversed != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWhenBinding()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;

                           public class MyDependency { }

                           public interface IMyService { void DoSomething(); }

                           public class MyService : IMyService
                           {
                               public MyDependency MyDependency { get; }
                               public MyService(MyDependency myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public partial class CompositionInOtherProject
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency>()
                                       .Bind().To<MyService>()
                                       .Root<IMyService>("MyService");

                               [Export(typeof(IMyService))]
                               public IMyService MyServiceRoot => MyService;
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService MyService { get; }

                               public Program(IMyService myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program = composition.Program;
                                   System.Console.WriteLine(program.MyService != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWithTagsLifetimeAndExportType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           public class MyDependency
                           {
                               public Guid Id { get; } = Guid.NewGuid();
                           }

                           public interface IMyService
                           {
                               MyDependency MyDependency { get; }
                               void DoSomething();
                           }

                           public class MyService : IMyService
                           {
                               public MyDependency MyDependency { get; }
                               public MyService(MyDependency myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public partial class CompositionInOtherProject
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency>()
                                       .Bind<IMyService>("svc").As(Lifetime.Singleton).To<MyService>()
                                       .Root<IMyService>("MyService", "svc", kind: RootKinds.Exported);
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService MyService { get; }

                               public Program([Tag("svc")] IMyService myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program1 = composition.Program;
                                   var program2 = composition.Program;
                                   Console.WriteLine(ReferenceEquals(program1.MyService, program2.MyService));
                                   Console.WriteLine(ReferenceEquals(program1.MyService.MyDependency, program2.MyService.MyDependency));
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWhenBindingWithTagsLifetimeAndExportType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           public class MyDependency
                           {
                               public Guid Id { get; } = Guid.NewGuid();
                           }

                           public interface IMyService
                           {
                               MyDependency MyDependency { get; }
                               void DoSomething();
                           }

                           public class MyService : IMyService
                           {
                               public MyDependency MyDependency { get; }
                               public MyService(MyDependency myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public partial class CompositionInOtherProject
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency>()
                                       .Bind().As(Lifetime.Singleton).To<MyService>()
                                       .Root<IMyService>("MyService");

                               [Export(typeof(IMyService), Lifetime.Singleton, "svc")]
                               public MyService MyServiceRoot => (MyService)MyService;
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService MyService { get; }

                               public Program([Tag("svc")] IMyService myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program1 = composition.Program;
                                   var program2 = composition.Program;
                                   Console.WriteLine(ReferenceEquals(program1.MyService, program2.MyService));
                                   Console.WriteLine(ReferenceEquals(program1.MyService.MyDependency, program2.MyService.MyDependency));
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportExportedRootFromOtherCompositionWhenBindingWithTagsLifetimeAndExportTypeWithGenericConstraints()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           public interface IMyDependency
                           {
                               Guid Id { get; }
                           }

                           public class MyDependency<T> : IMyDependency where T : class, new()
                           {
                               public Guid Id { get; } = Guid.NewGuid();
                           }

                           public interface IMyService<T> where T : class, new()
                           {
                               IMyDependency MyDependency { get; }
                               void DoSomething();
                           }

                           public class MyService<T> : IMyService<T> where T : class, new()
                           {
                               public IMyDependency MyDependency { get; }
                               public MyService(IMyDependency myDependency) => MyDependency = myDependency;
                               public void DoSomething() { }
                           }

                           public class MyModel { }

                           public partial class CompositionInOtherProject<T> where T : class, new()
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<MyDependency<T>>()
                                       .Bind().As(Lifetime.Singleton).To<MyService<T>>()
                                       .Root<IMyService<T>>("MyService");

                               [Export(typeof(IMyService<Pure.DI.TTN>), Lifetime.Singleton, "svc")]
                               public MyService<T> MyServiceRoot => (MyService<T>)MyService;
                           }

                           public partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup()
                                       .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject<MyModel>>()
                                       .Root<Program>("Program");
                           }

                           public partial class Program
                           {
                               public IMyService<MyModel> MyService { get; }

                               public Program([Tag("svc")] IMyService<MyModel> myService)
                               {
                                   MyService = myService;
                               }
                               
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var program1 = composition.Program;
                                   var program2 = composition.Program;
                                   Console.WriteLine(ReferenceEquals(program1.MyService, program2.MyService));
                                   Console.WriteLine(ReferenceEquals(program1.MyService.MyDependency, program2.MyService.MyDependency));
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }
}
