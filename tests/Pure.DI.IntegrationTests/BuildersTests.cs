namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the custom builders for the composition.
/// </summary>
public class BuildersTests
{
    [Fact]
    public async Task ShouldSupportBuilder()
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
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .Builder<Service>("BuildUpService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.BuildUpService(new Service());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuilderForInheritedTypesOnly()
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
                           
                               class BaseService
                               {
                                   public BaseService(int num)
                                   {
                                   }
                               }
                           
                               class Service: BaseService 
                               {
                                   public Service(): base(1) {}
                                   
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 1 {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                               
                               class Service2: BaseService 
                               {
                                   public Service2(): base(1) {}
                               
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 2 {depName}");
                                   }
                               
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .Builders<BaseService>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.BuildUp(new Service());
                                       var service2 = composition.BuildUp(new Service2());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 2 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuilders()
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
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 1 {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                               
                               class Service2: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 2 {depName}");
                                   }
                               
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .Builders<IService>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.BuildUp(new Service());
                                       var service2 = composition.BuildUp(new Service2());
                                       var service3 = composition.BuildUp((IService)new Service());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 2 Abc", "Initialize 1 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildersWhenFilter()
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
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize(int depName)
                                   {
                                       Console.WriteLine($"Initialize 1 {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                               
                               class Service2: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 2 {depName}");
                                   }
                               
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .Builders<IService>(filter: "*Service2");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service2 = composition.BuildUp(new Service2());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 2 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildersWhenGeneric()
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
                           
                               interface IService<T>
                               {
                                   IDependency? Dep { get; }
                               }
                           
                               class Service<T>: IService<T> 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 1 {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                               
                               class Service2<T>: IService<T> 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 2 {depName}");
                                   }
                               
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .Builders<IService<TT>>("BuildUpService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.BuildUpService<int>(new Service<int>());
                                       var service2 = composition.BuildUpService<int>(new Service2<int>());
                                       var service3 = composition.BuildUpService<int>((IService<int>)new Service2<int>());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 2 Abc", "Initialize 2 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildersWhenGenericAndSeveralTypeParams()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> { }
                               
                               class Dependency<T> : IDependency<T> { }
                               
                               interface IService<out T, T2>
                               {
                                   T Id { get; }
                                   
                                   IDependency<T2>? Dependency { get; }
                               }
                               
                               class Service1<T, T2>: IService<T, T2>
                                   where T: struct
                               {
                                   public T Id { get; private set; }
                                   
                                   [Ordinal(0)]
                                   public IDependency<T2>? Dependency { get; set; }
                               
                                   [Ordinal(1)]
                                   public void SetId([Tag(Tag.Id)] T id) => Id = id;
                               }
                               
                               class Service2<T, T2>: IService<T, T2>
                                   where T: struct
                               {
                                   public T Id { get; }
                               
                                   [Ordinal(0)]
                                   public IDependency<T2>? Dependency { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(Tag.Id).To<TT>(_ => (TT)(object)Guid.NewGuid())
                                           .Bind().To<Dependency<TT>>()
                                           // Generic service builder
                                           .Builders<IService<TT, TT2>>("BuildUpGeneric");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                        var composition = new Composition();
                                        var service1 = composition.BuildUpGeneric(new Service1<Guid, string>());
                                        var service2 = composition.BuildUpGeneric(new Service2<Guid, int>());
                                        var service3 = composition.BuildUpGeneric((IService<Guid, int>)new Service2<Guid, int>());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportBuildersWhenGenericAndSeveralTypeParamsAndIncompatibleConstraints()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           #pragma warning disable CS8618

                           namespace Sample
                           {
                               interface IDependency<T> { }
                               
                               class Dependency<T> : IDependency<T> { }
                               
                               interface IService<out T, T2>
                               {
                                   T Id { get; }
                                   
                                   IDependency<T2>? Dependency { get; }
                               }
                               
                               class Service1<T, T2>: IService<T, T2>
                                   where T: struct
                               {
                                   public T Id { get; private set; }
                                   
                                   [Ordinal(0)]
                                   public IDependency<T2>? Dependency { get; set; }
                               
                                   [Ordinal(1)]
                                   public void SetId([Tag(Tag.Id)] T id) => Id = id;
                               }
                               
                               class Service2<T, T2>: IService<T, T2>
                                   where T: class
                               {
                                   public T Id { get; }
                               
                                   [Ordinal(0)]
                                   public IDependency<T2>? Dependency { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(Tag.Id).To<TT>(_ => (TT)(object)Guid.NewGuid())
                                           .Bind().To<Dependency<TT>>()
                                           // Generic service builder
                                           .Builders<IService<TT, TT2>>("BuildUpGeneric");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                        var composition = new Composition();
                                        var service1 = composition.BuildUpGeneric(new Service1<Guid, string>());
                                        var service2 = composition.BuildUpGeneric(new Service2<string, int>());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportBuildersWhenName()
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
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 1 {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                               
                               class Service2: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 2 {depName}");
                                   }
                               
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .Builders<IService>("BuildUpService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.BuildUpService(new Service());
                                       var service2 = composition.BuildUpService(new Service2());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 2 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildersWhenNameTemplate()
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
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 1 {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                               
                               class Service2: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 2 {depName}");
                                   }
                               
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .Builders<IService>("BuildUp{type}");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.BuildUpService(new Service());
                                       var service2 = composition.BuildUpService2(new Service2());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 2 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuilderWhenDefaultName()
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
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .Builder<Service>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.BuildUp(new Service());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuilderWhenGeneric()
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
                           
                               interface IService<T>
                               {
                                   IDependency? Dep { get; }
                               }
                           
                               class Service<T>: IService<T> 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .Builder<Service<TT>>("BuildUpService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.BuildUpService<int>(new Service<int>());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuilderWhenSeveral()
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
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 1 {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                               
                               class Service2: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 2 {depName}");
                                   }
                               
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .Builder<Service>("BuildUpService")
                                           .Builder<Service2>("BuildUpService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.BuildUpService(new Service());
                                       var service2 = composition.BuildUpService(new Service2());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 2 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportCommonBuilderWhenTypesHierarchy()
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
                               
                               interface IService
                               {
                                   Guid Id { get; }
                                   
                                   IDependency? Dependency { get; }
                               }
                               
                               class Service1: IService
                               {
                                   public Guid Id { get; private set; } = Guid.Empty;
                               
                                   // The Dependency attribute specifies to perform an injection
                                   [Dependency]
                                   public IDependency? Dependency { get; set; }
                               
                                   [Dependency]
                                   public void SetId(Guid id) => Id = id;
                                   
                                   [Ordinal(1)]
                                   internal void Initialize()
                                   {
                                       Console.WriteLine($"Initialize 1");
                                   }
                               }
                               
                               class Service11: Service1 { }
                               class Service12: Service1 { }
                               class Service121: Service12 { }
                               
                               class Service2 : IService
                               {
                                   public Guid Id => Guid.Empty;
                               
                                   [Dependency]
                                   public IDependency? Dependency { get; set; }
                                   
                                   [Ordinal(1)]
                                   internal void Initialize()
                                   {
                                       Console.WriteLine($"Initialize 2");
                                   }
                               }
                               
                               class Service21: Service2 { }
                               class Service22: Service2 { }
                               class Service212: Service21 { }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To(_ => Guid.NewGuid())
                                           .Bind().To<Dependency>()
                                           .Builders<IService>("BuildUp");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.BuildUp((IService)new Service1());
                                       var service2 = composition.BuildUp((IService)new Service22());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1", "Initialize 2"], result);
    }
    [Fact]
    public async Task ShouldSupportBuilderForValueType()
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
                           
                               struct Service
                               {
                                   [Ordinal(0)]
                                   public IDependency Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Builder<Service>("BuildUpService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = new Service();
                                       service = composition.BuildUpService(service);
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
    public async Task ShouldSupportBuilderWithMethodInjectionAndTags()
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
                                   public IDependency Dep { get; private set; }

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
                                           .Builder<Service>("BuildUpService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = new Service();
                                       composition.BuildUpService(service);
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