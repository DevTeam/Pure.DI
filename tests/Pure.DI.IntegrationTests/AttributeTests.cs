namespace Pure.DI.IntegrationTests;

public class AttributeTests
{
    [Fact]
    public async Task ShouldSupportTypeAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency { }
                           
                               class AbcDependency : IDependency { }
                           
                               class XyzDependency : IDependency { }
                           
                               interface IService
                               {
                                   IDependency Dependency1 { get; }
                                   
                                   IDependency Dependency2 { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service(
                                       [Type(typeof(AbcDependency))] IDependency dependency1,
                                       [Type(typeof(XyzDependency))] IDependency dependency2)
                                   {
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                   }
                           
                                   public IDependency Dependency1 { get; }
                           
                                   public IDependency Dependency2 { get; }
                               }
                           
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IService>().To<Service>()
                           
                                           // Composition root
                                           .Root<IService>("Root");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependency1);
                                       Console.WriteLine(service.Dependency2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.AbcDependency", "Sample.XyzDependency"], result);
    }

    [Fact]
    public async Task ShouldSupportCustomTagAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
                               class MyTagAttribute : Attribute
                               {
                                   public MyTagAttribute(object tag) { }
                               }

                               interface IDependency { }
                           
                               class AbcDependency : IDependency { }
                           
                               class XyzDependency : IDependency { }
                           
                               interface IService
                               {
                                   IDependency Dependency1 { get; }
                                   
                                   IDependency Dependency2 { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service(
                                       [MyTag("Abc")] IDependency dependency1,
                                       [MyTag("Xyz")] IDependency dependency2)
                                   {
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                   }
                           
                                   public IDependency Dependency1 { get; }
                                   
                                   public IDependency Dependency2 { get; }
                               }
                           
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .TagAttribute<MyTagAttribute>()
                                           .Bind<IDependency>("Abc").To<AbcDependency>()
                                           .Bind<IDependency>("Xyz").To<XyzDependency>()
                                           .Bind<IService>().To<Service>()
                           
                                           // Composition root
                                           .Root<IService>("Root");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependency1);
                                       Console.WriteLine(service.Dependency2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.AbcDependency", "Sample.XyzDependency"], result);
    }

    [Fact]
    public async Task ShouldSupportTagAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency { }
                           
                               class AbcDependency : IDependency { }
                           
                               class XyzDependency : IDependency { }
                           
                               interface IService
                               {
                                   IDependency Dependency1 { get; }
                                   
                                   IDependency Dependency2 { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service(
                                       [Tag("Abc")] IDependency dependency1,
                                       [Tag("Xyz")] IDependency dependency2)
                                   {
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                   }
                           
                                   public IDependency Dependency1 { get; }
                                   
                                   public IDependency Dependency2 { get; }
                               }
                           
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency>("Abc").To<AbcDependency>()
                                           .Bind<IDependency>("Xyz").To<XyzDependency>()
                                           .Bind<IService>().To<Service>()
                           
                                           // Composition root
                                           .Root<IService>("Root");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependency1);
                                       Console.WriteLine(service.Dependency2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.AbcDependency", "Sample.XyzDependency"], result);
    }

    [Fact]
    public async Task ShouldSupportTagAttributeOnMembers()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency { }
                           
                               class AbcDependency : IDependency { }
                           
                               class XyzDependency : IDependency { }
                           
                               interface IService
                               {
                                   IDependency Dependency1 { get; }
                                   
                                   IDependency Dependency2 { get; }
                               }
                           
                               class Service : IService
                               {
                                   [Tag("Abc"), Ordinal(1)]
                                   public IDependency Dependency1 { get; set; } = null!;
                                   
                                   [Tag("Xyz"), Ordinal(2)]
                                   public IDependency Dependency2 = null!;

                                   IDependency IService.Dependency1 => Dependency1;
                                   IDependency IService.Dependency2 => Dependency2;
                               }
                           
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency>("Abc").To<AbcDependency>()
                                           .Bind<IDependency>("Xyz").To<XyzDependency>()
                                           .Bind<IService>().To<Service>()
                           
                                           // Composition root
                                           .Root<IService>("Root");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependency1);
                                       Console.WriteLine(service.Dependency2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.AbcDependency", "Sample.XyzDependency"], result);
    }

    [Fact]
    public async Task ShouldSupportTypeAttributeOnMembers()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency { }
                           
                               class AbcDependency : IDependency { }
                           
                               class XyzDependency : IDependency { }
                           
                               interface IService
                               {
                                   IDependency Dependency1 { get; }
                                   
                                   IDependency Dependency2 { get; }
                               }
                           
                               class Service : IService
                               {
                                   [Type(typeof(AbcDependency)), Ordinal(1)]
                                   public IDependency Dependency1 { get; set; } = null!;
                                   
                                   [Type(typeof(XyzDependency)), Ordinal(2)]
                                   public IDependency Dependency2 = null!;

                                   IDependency IService.Dependency1 => Dependency1;
                                   IDependency IService.Dependency2 => Dependency2;
                               }
                           
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IService>().To<Service>()
                           
                                           // Composition root
                                           .Root<IService>("Root");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependency1);
                                       Console.WriteLine(service.Dependency2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.AbcDependency", "Sample.XyzDependency"], result);
    }

    [Fact]
    public async Task ShouldSupportConstructorOrdinalAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency { }
                           
                               class Dependency : IDependency { }
                           
                               interface IService { string State { get; } }
                           
                               class Service : IService
                               {
                                   public Service() => State = "default";

                                   [Ordinal(1)]
                                   public Service(IDependency dependency) => State = "ordinal";

                                   public string State { get; }
                               }
                           
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.State);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["ordinal"], result);
    }

    [Fact]
    public async Task ShouldSupportMethodInjectionWithOrdinalAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency { }
                           
                               class Dependency : IDependency { }
                           
                               interface IService { IDependency Dep { get; } }
                           
                               class Service : IService
                               {
                                   public IDependency Dep { get; private set; } = null!;

                                   [Ordinal(1)]
                                   public void Initialize(IDependency dependency)
                                   {
                                       Dep = dependency;
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
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
    internal async Task ShouldSupportOrdinalAttributesWithoutAnyConstructorArg()
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
                           
                                   IDependency OtherDep0 { get; }
                           
                                   IDependency OtherDep1 { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private IDependency _dep1;
                                   [Ordinal(1)] public IDependency? _otherDep1;
                                   [Ordinal] public IDependency? _otherDep0;
                           
                                   public Service(IDependency dep)
                                   { 
                                       _dep1 = dep;
                                   }
                           
                                   public IDependency Dep => _dep1;
                                   public IDependency OtherDep0 => _otherDep0!;
                                   public IDependency OtherDep1 => _otherDep1!;
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
                                       Console.WriteLine(service.Dep != service.OtherDep0);
                                       Console.WriteLine(service.Dep != service.OtherDep1);          
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    internal async Task ShouldSupportOrdinalCustomAttributesWithoutAnyConstructorArg()
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
                                   public CustomOrdinalAttribute()
                                   {
                                   }
                               }
                           
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {        
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                           
                                   IDependency OtherDep0 { get; }
                           
                                   IDependency OtherDep1 { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private IDependency _dep1;
                                   [Ordinal(1)] public IDependency? _otherDep1;
                                   [CustomOrdinal] public IDependency? _otherDep0;
                           
                                   public Service(IDependency dep)
                                   { 
                                       _dep1 = dep;
                                   }
                           
                                   public IDependency Dep => _dep1;
                                   public IDependency OtherDep0 => _otherDep0!;
                                   public IDependency OtherDep1 => _otherDep1!;
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
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
                                       Console.WriteLine(service.Dep != service.OtherDep0);
                                       Console.WriteLine(service.Dep != service.OtherDep1);          
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
    public async Task ShouldSupportGenericTypeAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
                               internal class TypeAttribute<T> : Attribute
                               {
                               }
                           
                               interface IDependency { }
                           
                               class AbcDependency : IDependency { }
                           
                               class XyzDependency : IDependency { }
                           
                               interface IService
                               {
                                   IDependency Dependency1 { get; }
                                   
                                   IDependency Dependency2 { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service(
                                       [Type<AbcDependency>] IDependency dependency1,
                                       [Type<XyzDependency>] IDependency dependency2)
                                   {
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                   }
                           
                                   public IDependency Dependency1 { get; }
                           
                                   public IDependency Dependency2 { get; }
                               }
                           
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .TypeAttribute<TypeAttribute<TT>>()
                                           .Bind<IService>().To<Service>()
                           
                                           // Composition root
                                           .Root<IService>("Root");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependency1);
                                       Console.WriteLine(service.Dependency2);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.AbcDependency", "Sample.XyzDependency"], result);
    }

    [Fact]
    public async Task ShouldSupportCombinedGenericAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               [AttributeUsage(
                                   AttributeTargets.Constructor
                                   | AttributeTargets.Method
                                   | AttributeTargets.Parameter
                                   | AttributeTargets.Property
                                   | AttributeTargets.Field)]
                               class InjectAttribute<T> : Attribute
                               {
                                   public InjectAttribute(object? tag = null, int ordinal = 0)
                                   {
                                   }
                               }
                           
                               interface IPerson {}
                           
                               class Person : IPerson
                               {
                                   private object? _state;
                                   
                                   [Inject<int>]
                                   internal object Id = "";
                           
                                   private readonly string _name;
                           
                                   public Person([Inject<string>("NikName")] string name)
                                   {
                                       _name = name;
                                   }
                           
                                   public void Initialize([Inject<Uri>] object state) => 
                                       _state = state;
                           
                                   public override string ToString() => $"{Id} {_name} {_state}";
                               }
                           
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .TagAttribute<InjectAttribute<TT>>(0)
                                           .OrdinalAttribute<InjectAttribute<TT>>(1)
                                           .TypeAttribute<InjectAttribute<TT>>()
                                           .Arg<int>("personId")
                                           .Bind().To(_ => new Uri("https://github.com/DevTeam/Pure.DI"))
                                           .Bind("NikName").To(_ => "Nik")
                                           .Bind().To<Person>()
                           
                                           // Composition root
                                           .Root<IPerson>("Person");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(personId: 123);
                                       var person = composition.Person;
                                       Console.WriteLine(person);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["123 Nik https://github.com/DevTeam/Pure.DI"], result);
    }
#endif
}