namespace Pure.DI.IntegrationTests;

using Core;

public class TagsTests
{
    [Fact]
    public async Task ShouldSupportTags()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>().To(_ => "1")
                                           .Bind<string>(2).To(_ => "2")
                                           .Bind<string>('3').To(_ => "3")
                                           .Bind<string>("4").To(_ => "4")
                                           .Root<string>("Result")
                                           .Root<string>("Result2", 2)
                                           .Root<string>("Result3", '3')
                                           .Root<string>("Result4", "4");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result);
                                       Console.WriteLine(composition.Result2);
                                       Console.WriteLine(composition.Result3);
                                       Console.WriteLine(composition.Result4);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2", "3", "4"], result);
    }

    [Fact]
    public async Task ShouldSupportTagsForSeveralRoots()
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
                                      
                               class Dependency : IDependency { }
                           
                               interface IService
                               {
                                  IDependency Dependency1 { get; }
                           
                                  IDependency Dependency2 { get; }
                                  
                                  IDependency Dependency3 { get; }
                               }
                           
                               class Service : IService
                               {
                                  public Service(
                                      [Tag("Abc")] IDependency dependency1,
                                      [Tag("Xyz")] IDependency dependency2,
                                      IDependency dependency3)
                                  {
                                      Dependency1 = dependency1;
                                      Dependency2 = dependency2;
                                      Dependency3 = dependency3;
                                  }
                           
                                  public IDependency Dependency1 { get; }
                           
                                  public IDependency Dependency2 { get; }
                                  
                                  public IDependency Dependency3 { get; }
                               }
                           
                               static class Setup
                               {
                                  private static void SetupComposition()
                                  {
                                      DI.Setup("Composition")
                                           .Bind<IDependency>("Abc", default).To<AbcDependency>()
                                           .Bind<IDependency>("Xyz")
                                           .As(Lifetime.Singleton)
                                           .To<XyzDependency>()
                                           .Root<IDependency>("XyzRoot", "Xyz")
                                           .Bind<IService>().To<Service>().Root<IService>("Root");
                                  }
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      var service = composition.Root;
                                      Console.WriteLine(service.Dependency1?.GetType() == typeof(AbcDependency));
                                      Console.WriteLine(service.Dependency2?.GetType() == typeof(XyzDependency));
                                      Console.WriteLine(service.Dependency2 == composition.XyzRoot);
                                      Console.WriteLine(service.Dependency3?.GetType() == typeof(AbcDependency));
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportTagsWhenEnum()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public enum MyEnum
                               {
                                   Option2
                               };
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>().To(_ => "1")
                                           .Bind<string>(MyEnum.Option2).To(_ => "2")
                                           .Root<string>("Result")
                                           .Root<string>("Result2", MyEnum.Option2);
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result);
                                       Console.WriteLine(composition.Result2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2"], result);
    }

    [Fact]
    public async Task ShouldSupportTagsWhenType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>().To(_ => "1")
                                           .Bind<string>(typeof(int)).To(_ => "2")
                                           .Root<string>("Result")
                                           .Root<string>("Result2", typeof(int));
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result);
                                       Console.WriteLine(composition.Result2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2"], result);
    }

    [Fact]
    public async Task ShouldSupportTagsWhenNull()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>(1, null).To(_ => "1")
                                           .Bind<string>(2).To(_ => "2")
                                           .Root<string>("Result")
                                           .Root<string>("Result2", 2);
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result);
                                       Console.WriteLine(composition.Result2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2"], result);
    }

    [Fact]
    public async Task ShouldSupportTagsWhenDefault()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>(1, default).To(_ => "1")
                                           .Bind<string>(2).To(_ => "2")
                                           .Root<string>("Result")
                                           .Root<string>("Result2", 2);
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result);
                                       Console.WriteLine(composition.Result2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2"], result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldSupportTagsAsArrayInBind()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To(ctx => 1)
                                           .Bind([1, 2]).To(ctx => 2)
                                           .Root<int>("Root")
                                           .Root<int>("Root2", 1)
                                           .Root<int>("Root3", 2);
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root);
                                       Console.WriteLine(composition.Root2);
                                       Console.WriteLine(composition.Root3);
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp12 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2", "2"], result);
    }

    [Fact]
    public async Task ShouldSupportTagsAsArrayInTagsMethod()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To(ctx => 1)
                                           .Bind().Tags([1, 2]).To(ctx => 2)
                                           .Root<int>("Root")
                                           .Root<int>("Root2", 1)
                                           .Root<int>("Root3", 2);
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root);
                                       Console.WriteLine(composition.Root2);
                                       Console.WriteLine(composition.Root3);
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp12 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2", "2"], result);
    }
#endif

    [Fact]
    public async Task ShouldSupportTagUnique()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using System.Linq;
                               using System.Collections.Generic;
                               using Pure.DI;
                               using Sample;
                               
                               internal interface IDep { }
                               
                               internal class Dep1: IDep { }
                               
                               internal class Dep2: IDep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(IEnumerable<IDep> deps)
                                   {
                                       Console.WriteLine(deps.Count());
                                   }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind<IDep>(Tag.Unique).To<Dep1>()
                                           .Bind(Tag.Unique).To<Dep2>()
                                           .Bind().To<Service>()
                                           .Root<Service>("Root");
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2", "Sample.Service"], result);
    }
    
    [Fact]
    public async Task ShouldSupportTagUniqueWhenStatic()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using System.Linq;
                               using System.Collections.Generic;
                               using Pure.DI;
                               using Sample;
                               using static Pure.DI.Tag;
                               
                               internal interface IDep { }
                               
                               internal class Dep1: IDep { }
                               
                               internal class Dep2: IDep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(IEnumerable<IDep> deps)
                                   {
                                       Console.WriteLine(deps.Count());
                                   }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind<IDep>(Unique).To<Dep1>()
                                           .Bind(Unique).To<Dep2>()
                                           .Bind().To<Service>()
                                           .Root<Service>("Root");
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2", "Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportTagType()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal class Dep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service([Tag(typeof(Dep))] Dep dep) { }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind<Dep>(Tag.Type).To<Dep>()
                                           .Bind<IService>(Tag.Type).To<Service>()
                                           .Root<IService>("Root1", typeof(Service))
                                           .Root<Service>("Root2", typeof(Service)); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root1);
                                      Console.WriteLine(composition.Root2);
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service", "Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportTagOnWhenCtor()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal class Dep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(Dep dep) { }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("Sample.Service.Service:dep")).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportOnConstructorArg()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal class Dep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(Dep dep) { }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.OnConstructorArg<Service>("dep")).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportTagOnWhenInterfaceInCtor()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal interface IDep { }
                               
                               internal class Dep: IDep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(IDep dep) { }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("Sample.Service.Service:dep")).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportTagOnWhenDecorator()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal interface IService { }
                               
                               internal class BaseService: IService
                               {
                               }
                           
                               internal class Service: IService
                               {
                                   public Service(IService baseService) { }
                               }
                               
                               internal partial class Composition
                               {
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("*.Service:baseService")).To<BaseService>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportTagOnWhenGenericInCtor()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal interface IAbc { }
                               
                               internal interface IDep { }
                               
                               internal class Dep: IDep { }
                           
                               internal interface IService<T> { }
                           
                               internal class Service<T>: IService<T>
                               {
                                   public Service(IDep dep) { }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Hint(Hint.Resolve, "Off")
                                           .Bind(Tag.On("Sample.Service`1.Service:dep")).To<Dep>()
                                           .Bind().To<Service<TT>>()
                                           .Root<IService<TT4>>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root<int>());
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service`1[System.Int32]"], result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldSupportSeveralTagOn()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal interface IDep { }
                               
                               internal class Dep: IDep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(Dep dep, IDep abc) { }
                                   private IDep? _myProp;
                                   
                                   public required IDep MyProp
                                   {
                                       init 
                                       {
                                           _myProp = value;
                                           Console.WriteLine(value);
                                       }
                                       
                                       get
                                       {
                                           return _myProp!;
                                       }
                                   }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("Sample.Service.Service:dep"), Tag.On("Sample.Service.Service:abc"), Tag.On("Sample.Service:MyProp")).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dep", "Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportCombinedTagOn()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal interface IDep { }
                               
                               internal class Dep: IDep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(Dep dep, IDep abc) { }
                                   private IDep? _myProp;
                                   
                                   public required IDep MyProp
                                   {
                                       init 
                                       {
                                           _myProp = value;
                                           Console.WriteLine(value);
                                       }
                                       
                                       get
                                       {
                                           return _myProp!;
                                       }
                                   }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("Sample.Service.Service:dep", "Sample.Service.Service:abc", "Sample.Service:MyProp")).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dep", "Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportOnMember()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal interface IDep { }
                               
                               internal class Dep: IDep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(Dep dep, IDep abc) { }
                                   private IDep? _myProp;
                                   
                                   public required IDep MyProp
                                   {
                                       init 
                                       {
                                           _myProp = value;
                                           Console.WriteLine(value);
                                       }
                                       
                                       get
                                       {
                                           return _myProp!;
                                       }
                                   }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("Sample.Service.Service:dep", "Sample.Service.Service:abc"), Tag.OnMember<Service>(nameof(Service.MyProp))).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dep", "Sample.Service"], result);
    }


    [Fact]
    public async Task ShouldSupportOnMethodArg()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal interface IDep { }
                               
                               internal class Dep: IDep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(Dep dep, IDep abc) { }
                                   private IDep? _myProp;
                                   
                                   [Ordinal(1)]
                                   public void Init(IDep dep)
                                   {
                                       _myProp = dep;
                                       Console.WriteLine(dep);
                                   }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("Sample.Service.Service:dep", "Sample.Service.Service:abc"), Tag.OnMethodArg<Service>(nameof(Service.Init), "dep")).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dep", "Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportTagOnWithWildcard()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal interface IDep { }
                               
                               internal class Dep: IDep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(Dep dep, IDep abc) { }
                                   private IDep? _myProp;
                                   
                                   public required IDep MyProp
                                   {
                                       init 
                                       {
                                           _myProp = value;
                                           Console.WriteLine(value);
                                       }
                                       
                                       get
                                       {
                                           return _myProp!;
                                       }
                                   }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("Sample.Service.Service:*"), Tag.On("Sample.Service:MyProp")).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dep", "Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportTagOnWhenRequiredProperty()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal interface IDep { }
                               
                               internal class Dep: IDep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   private IDep? _myProp;
                           
                                   public required IDep MyProp
                                   {
                                       init 
                                       {
                                           _myProp = value;
                                       }
                                       
                                       get
                                       {
                                           return _myProp!;
                                       }
                                   }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("Sample.Service:MyProp")).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<Service>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                      Console.WriteLine(composition.Root.MyProp);
                                  }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service", "Sample.Dep"], result);
    }

    [Fact]
    public async Task ShouldSupportTagOnWhenRequiredField()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal class Dep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public required Dep? MyField;
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind("Sample.Service:MyField").To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<Service>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                      Console.WriteLine(composition.Root.MyField);
                                  }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service", "Sample.Dep"], result);
    }

    [Fact]
    public async Task ShouldSupportTagOnForComplex()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               interface IDependency { };
                           
                               class AbcDependency : IDependency { };
                           
                               class XyzDependency : IDependency { };
                           
                               class Consumer<T>
                               {
                                   public Consumer(IDependency myDep)
                                   {
                                       Dependency = myDep;
                                   }
                           
                                   public IDependency Dependency { get; }
                               }
                                       
                               interface IService
                               {
                                   IDependency Dependency1 { get; }
                           
                                   IDependency Dependency2 { get; }
                                   
                                   IDependency Dependency3 { get; }
                                   
                                   IDependency Dependency4 { get; }
                               }
                           
                               class Service : IService
                               {
                                   private readonly Consumer<string> _consumer;
                           
                                   public Service(IDependency dependency1,
                                       IDependency dependency2,
                                       Consumer<string> consumer)
                                   {
                                       _consumer = consumer;
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                   }
                           
                                   public IDependency Dependency1 { get; }
                           
                                   public IDependency Dependency2 { get; }
                           
                                   public required IDependency Dependency3 { init; get; }
                           
                                   public IDependency Dependency4 => _consumer.Dependency;
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("*Service:Dependency3", "*Consumer:myDep"))
                                               .To<AbcDependency>()
                                           .Bind(Tag.On("*Service:dependency?"))
                                               .To<XyzDependency>()
                                           .Bind().To<Consumer<TT>>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependency1);
                                       Console.WriteLine(service.Dependency2);
                                       Console.WriteLine(service.Dependency3);
                                       Console.WriteLine(service.Dependency4);
                                  }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.XyzDependency", "Sample.XyzDependency", "Sample.AbcDependency", "Sample.AbcDependency"], result);
    }

    [Fact]
    public async Task ShouldSupportTagOnOrderForComplex()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               interface IDependency { };
                           
                               class AbcDependency : IDependency { };
                           
                               class XyzDependency : IDependency { };
                           
                               class Consumer<T>
                               {
                                   public Consumer(IDependency myDep)
                                   {
                                       Dependency = myDep;
                                   }
                           
                                   public IDependency Dependency { get; }
                               }
                                       
                               interface IService
                               {
                                   IDependency Dependency1 { get; }
                           
                                   IDependency Dependency2 { get; }
                                   
                                   IDependency Dependency3 { get; }
                                   
                                   IDependency Dependency4 { get; }
                               }
                           
                               class Service : IService
                               {
                                   private readonly Consumer<string> _consumer;
                           
                                   public Service(IDependency dependency1,
                                       IDependency dependency2,
                                       Consumer<string> consumer)
                                   {
                                       _consumer = consumer;
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                   }
                           
                                   public IDependency Dependency1 { get; }
                           
                                   public IDependency Dependency2 { get; }
                           
                                   public required IDependency Dependency3 { init; get; }
                           
                                   public IDependency Dependency4 => _consumer.Dependency;
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(Tag.On("*Consumer:myDep"))
                                               .To<AbcDependency>()
                                           .Bind(Tag.On("*Service:dependency?"))
                                               .To<XyzDependency>()
                                           .Bind(Tag.On("*Service:Dependency3"))
                                               .To<XyzDependency>()
                                           .Bind(Tag.On("*Service:dependency1"))
                                               .To<AbcDependency>()
                                           .Bind().To<Consumer<TT>>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependency1);
                                       Console.WriteLine(service.Dependency2);
                                       Console.WriteLine(service.Dependency3);
                                       Console.WriteLine(service.Dependency4);
                                  }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.AbcDependency", "Sample.XyzDependency", "Sample.XyzDependency", "Sample.AbcDependency"], result);
    }
#endif

    [Fact]
    public async Task ShouldShowWarningWhenTagOnWasNotUsed()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal class Dep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(Dep dep) { }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind().To<Dep>()
                                           .Bind(Tag.On("Sample.Service.Service:abc")).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0);
        result.Warnings.Count.ShouldBe(1);
        result.Warnings.Count(i => i.Id == LogId.WarningMetadataDefect).ShouldBe(1);
        result.StdOut.ShouldBe(["Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportTag()
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
                                   public Service([Tag(123)] Func<IDependency> dependency1, [Tag(123)] IDependency dependency2)
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
                                           .Bind<IDependency>(123).As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IDependency>("Dependency", 123)
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
    public async Task ShouldShowErrorWhenCannotResolveByTag()
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
                                   public Service(Func<IDependency> dependency1, [Tag(123)] IDependency dependency2)
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
                                           .Bind().As(Lifetime.Singleton).To<Dependency>()
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1);
    }
}