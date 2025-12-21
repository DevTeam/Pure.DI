namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the property injection.
/// </summary>
public class PropertyInjectionTests
{
    [Theory]
    [InlineData(Lifetime.Transient)]
    [InlineData(Lifetime.PerBlock)]
    [InlineData(Lifetime.Singleton)]
    [InlineData(Lifetime.Scoped)]
    [InlineData(Lifetime.PerResolve)]
    internal async Task ShouldSupportInitPropertyInjection(Lifetime lifetime)
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
                                   
                                   [CustomOrdinal(1)]
                                   public IDependency OtherDep1
                                   {
                                       init
                                       {
                                           Console.WriteLine("OtherDep1");
                                           Console.WriteLine(value != Dep);
                                       }
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency OtherDep0
                                   {
                                       set
                                       {
                                           Console.WriteLine("OtherDep0");
                                           Console.WriteLine(value != Dep);
                                       }
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().As(Lifetime.#lifetime#).To<Service>()
                                           .Root<IService>("Service")
                                           .OrdinalAttribute<CustomOrdinalAttribute>();
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
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["OtherDep1", "True", "OtherDep0", "True"], result);
    }

    [Theory]
    [InlineData(Lifetime.Transient)]
    [InlineData(Lifetime.PerBlock)]
    [InlineData(Lifetime.Singleton)]
    [InlineData(Lifetime.Scoped)]
    [InlineData(Lifetime.PerResolve)]
    internal async Task ShouldSupportPropertyInjection(Lifetime lifetime)
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
                                   
                                   [Ordinal(1)]
                                   public IDependency OtherDep1
                                   {
                                       set
                                       {
                                           Console.WriteLine("OtherDep1");
                                           Console.WriteLine(value != Dep);
                                       }
                                   }
                           
                                   [CustomOrdinal(0)]
                                   public IDependency OtherDep0
                                   {
                                       set
                                       {
                                           Console.WriteLine("OtherDep0");
                                           Console.WriteLine(value != Dep);
                                       }
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().As(Lifetime.#lifetime#).To<Service>()
                                           .Root<IService>("Service")
                                           .OrdinalAttribute<CustomOrdinalAttribute>();
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
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["OtherDep0", "True", "OtherDep1", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportRequiredPropertyInjection()
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
                                   
                                   public required IDependency OtherDep1
                                   {
                                       init 
                                       {
                                           Console.WriteLine("OtherDep1");
                                           Console.WriteLine(value != Dep);
                                       }
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency OtherDep0
                                   {
                                       init
                                       {
                                           Console.WriteLine("OtherDep0");
                                           Console.WriteLine(value != Dep);
                                       }
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["OtherDep0", "True", "OtherDep1", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportRequiredPropertyInjectionWhenBaseProperty()
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
                                   
                                   public required IDependency OtherDep1
                                   {
                                       init 
                                       {
                                           Console.WriteLine("OtherDep1");
                                           Console.WriteLine(value != Dep);
                                       }
                                   }
                               }
                               
                               class Service2: Service
                               {
                                   public Service2(IDependency dep)
                                       : base(dep)
                                   { 
                                   }
                                   
                                   [Ordinal(0)]
                                   public IDependency OtherDep0
                                   {
                                       init
                                       {
                                           Console.WriteLine("OtherDep0");
                                           Console.WriteLine(value != Dep);
                                       }
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service2>()
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["OtherDep0", "True", "OtherDep1", "True"], result);
    }
    [Fact]
    public async Task ShouldSupportPropertyInjectionWithTag()
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
                                   [Ordinal(0), Tag("myTag")]
                                   public IDependency? Dep { get; set; }
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
    public async Task ShouldSupportInternalPropertyInjection()
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
                                   [Ordinal(0)]
                                   internal IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
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
                                       Console.WriteLine(service.Dep != null);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }
}