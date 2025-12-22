namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the method injection.
/// </summary>
public class MethodInjectionTests
{
    [Theory]
    [InlineData(Lifetime.Transient, "Initialize dep", "Initialize dep", "Initialize", "False", "Activate")]
    [InlineData(Lifetime.PerBlock, "Initialize dep", "Initialize", "True", "Activate")]
    [InlineData(Lifetime.Singleton, "Initialize dep", "Initialize", "True", "Activate")]
    [InlineData(Lifetime.Scoped, "Initialize dep", "Initialize", "True", "Activate")]
    [InlineData(Lifetime.PerResolve, "Initialize dep", "Initialize", "True", "Activate")]
    internal async Task ShouldSupportMethodInjection(Lifetime lifetime, params string[] output)
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
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private IDependency _dep;
                                   public Service(IDependency dep)
                                   { 
                                       _dep = dep;
                                   }
                           
                                   public IDependency Dep => _dep;
                                   
                                   public void Run()
                                   {
                                       Console.WriteLine("Run");
                                   }
                           
                                   [Ordinal(7)]
                                   public void Activate()
                                   {
                                       Console.WriteLine("Activate");
                                   }
                           
                                   [Ordinal(1)]
                                   internal void Initialize(IDependency dep)
                                   {
                                       Console.WriteLine("Initialize");
                                       Console.WriteLine(dep == Dep);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.#lifetime#).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("depName", 374)
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("dep");
                                       var service = composition.Service;
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([..output], result);
    }

    [Fact]
    public async Task ShouldSupportMethodInjectionWhenBaseMethod()
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
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private IDependency _dep;
                                   public Service(IDependency dep)
                                   { 
                                       _dep = dep;
                                   }
                           
                                   public IDependency Dep => _dep;
                                   
                                   public void Run()
                                   {
                                       Console.WriteLine("Run");
                                   }
                           
                                   [Ordinal(1)]
                                   internal void Initialize(IDependency dep)
                                   {
                                       Console.WriteLine("Initialize");
                                       Console.WriteLine(dep != Dep);
                                   }
                               }
                               
                               class Service2: Service
                               {
                                   public Service2(IDependency dep)
                                       : base(dep)
                                   { 
                                   }
                                   
                                   [Ordinal(7)]
                                   public void Activate()
                                   {
                                       Console.WriteLine("Activate");
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service2>()
                                           .Arg<string>("depName", 374)
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("dep");
                                       var service = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize dep", "Initialize dep", "Initialize", "True", "Activate"], result);
    }

    [Fact]
    public async Task ShouldSupportMethodInjectionSequence()
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
                               }

                               interface IService
                               {
                               }

                               class Service: IService 
                               {
                                   [Ordinal(2)]
                                   public void Dep2(IDependency dep)
                                   {
                                       Console.WriteLine("Dep2");
                                   }

                                   [Ordinal(1)]
                                   public void Dep1(IDependency dep)
                                   {
                                       Console.WriteLine("Dep1");
                                   }

                                   [Ordinal(0)]
                                   public void Dep0(IDependency dep)
                                   {
                                       Console.WriteLine("Dep0");
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
        result.StdOut.ShouldBe(["Dep0", "Dep1", "Dep2"], result);
    }

    [Fact]
    public async Task ShouldSupportMethodInjectionWhenCustomAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
                               internal class CustomOrdinalAttribute : Attribute
                               {
                                   public readonly int Ordinal;
                           
                                   public CustomOrdinalAttribute(int ordinal)
                                   {
                                       Ordinal = ordinal;
                                   }
                               }
                               
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private IDependency _dep;
                                   public Service(IDependency dep)
                                   { 
                                       _dep = dep;
                                   }
                           
                                   public IDependency Dep => _dep;
                                   
                                   public void Run()
                                   {
                                       Console.WriteLine("Run");
                                   }
                           
                                   [Ordinal(7)]
                                   public void Activate()
                                   {
                                       Console.WriteLine("Activate");
                                   }
                           
                                   [CustomOrdinal(1)]
                                   internal void Initialize(IDependency dep)
                                   {
                                       Console.WriteLine("Initialize");
                                       Console.WriteLine(dep != Dep);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("depName", 374)
                                           .OrdinalAttribute<CustomOrdinalAttribute>()     
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("dep");
                                       var service = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize dep", "Initialize dep", "Initialize", "True", "Activate"], result);
    }

    [Fact]
    public async Task ShouldSupportMethodInjectionWhenOutParam()
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
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] out string depName)
                                   {
                                       depName = "";
                                       Console.WriteLine($"Initialize");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private IDependency _dep;
                                   public Service(IDependency dep)
                                   { 
                                       _dep = dep;
                                   }
                           
                                   public IDependency Dep => _dep;
                                   
                                   public void Run()
                                   {
                                       Console.WriteLine("Run");
                                   }
                           
                                   [Ordinal(7)]
                                   public void Activate()
                                   {
                                       Console.WriteLine("Activate");
                                   }
                           
                                   [Ordinal(1)]
                                   internal void Initialize(IDependency dep)
                                   {
                                       Console.WriteLine("Initialize");
                                       Console.WriteLine(dep != Dep);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("depName", 374)
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("dep");
                                       var service = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize", "Initialize", "Initialize", "True", "Activate"], result);
    }

    [Fact]
    public async Task ShouldSupportMethodInjectionWhenRefParam()
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
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] ref string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private IDependency _dep;
                                   public Service(IDependency dep)
                                   { 
                                       _dep = dep;
                                   }
                           
                                   public IDependency Dep => _dep;
                                   
                                   public void Run()
                                   {
                                       Console.WriteLine("Run");
                                   }
                           
                                   [Ordinal(7)]
                                   public void Activate()
                                   {
                                       Console.WriteLine("Activate");
                                   }
                           
                                   [Ordinal(1)]
                                   internal void Initialize(IDependency dep)
                                   {
                                       Console.WriteLine("Initialize");
                                       Console.WriteLine(dep != Dep);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("depName", 374)
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("dep");
                                       var service = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize dep", "Initialize dep", "Initialize", "True", "Activate"], result);
    }
    [Fact]
    public async Task ShouldSupportMethodInjectionWithTagOnParameter()
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
                           
                               class Service
                               {
                                   public IDependency? Dep { get; private set; }

                                   [Ordinal(0)]
                                   public void Init([Tag("myTag")] IDependency dep)
                                   {
                                       Dep = dep;
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>("myTag").To<Dependency>()
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
                                       Console.WriteLine(service.Dep != null);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportMethodInjectionForMultipleMethods()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Service
                               {
                                   [Ordinal(0)]
                                   public void Init1() => Console.WriteLine("Init1");

                                   [Ordinal(1)]
                                   public void Init2() => Console.WriteLine("Init2");
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
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
        result.StdOut.ShouldBe(["Init1", "Init2"], result);
    }
}