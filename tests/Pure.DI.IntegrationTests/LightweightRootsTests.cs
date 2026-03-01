namespace Pure.DI.IntegrationTests;

public class LightweightRootsTests
{
    [Fact]
    public async Task ShouldSupportLightweightRoot()
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

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().As(Lifetime.PerResolve).To<Dependency>()
                                           .Root<Service>("Root", kind: RootKinds.Light);
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Root;
                                       var service2 = composition.Resolve<Service>();
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 1 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportLightweightRootWhenNoName()
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

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().As(Lifetime.PerResolve).To<Dependency>()
                                           .Root<Service>(kind: RootKinds.Light);
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<Service>();
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportSeveralLightweightRoots()
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

                               class Xyz
                               {
                                    public Xyz()
                                    {
                                        Console.WriteLine("Xyz");
                                    }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().As(Lifetime.PerResolve).To<Dependency>()
                                           .Root<Service>("Root", kind: RootKinds.Light)
                                           .Root<Xyz>("Xyz", kind: RootKinds.Light);
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Root;
                                       var service2 = composition.Resolve<Service>();
                                       var xyz = composition.Xyz;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 1 Abc", "Xyz"], result);
    }

    [Fact]
    public async Task ShouldSupportLightweightRootWithStringArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { string Name { get; } }
                           class Service(string name) : IService { public string Name { get; } = name; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<string>("name")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService("abc").Name);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["abc"], result);
    }

    [Fact]
    public async Task ShouldSupportLightweightRootWithIntArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { int Id { get; } }
                           class Service : IService { public int Id { get; } = 77; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService(42).Id);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["42"], result);
    }

    [Fact]
    public async Task ShouldSupportLightweightRootWithCustomClassArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class User(string name) { public string Name { get; } = name; }
                           interface IService { string Name { get; } }
                           class Service(User user) : IService { public string Name { get; } = user.Name; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<User>("user")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService(new User("Bob")).Name);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Bob"], result);
    }

    [Fact]
    public async Task ShouldSupportLightweightRootWithTwoArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { string Value { get; } }
                           class Service(int id, string name) : IService { public string Value { get; } = $"{id}:{name}"; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .RootArg<string>("name")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Create(7, "Neo").Value);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["7:Neo"], result);
    }

    [Fact]
    public async Task ShouldSupportLightweightRootWithNamedArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { string Value { get; } }
                           class Service(int id, string name) : IService { public string Value { get; } = $"{id}:{name}"; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .RootArg<string>("name")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Create(name: "Trinity", id: 9).Value);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["9:Trinity"], result);
    }

    [Fact]
    public async Task ShouldSupportLightweightRootWithTaggedArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { string Token { get; } }
                           class Service([Tag("token")] string token) : IService { public string Token { get; } = token; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<string>("value", "token")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Create("t-1").Token);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["t-1"], result);
    }

    [Fact]
    public async Task ShouldSupportLightweightRootWithSeveralTaggedArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { string Value { get; } }
                           class Service([Tag("a")] int id, [Tag("b")] string name) : IService
                           {
                               public string Value { get; } = $"{id}:{name}";
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id", "a")
                                   .RootArg<string>("name", "b")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Create(1, "A").Value);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1:A"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericLightweightRootWithSingleTypeParameter()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> { string Name { get; } }
                           class Service<T> : IService<T> { public string Name { get; } = typeof(T).Name; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT>>().To<Service<TT>>()
                                   .Root<IService<TT>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<int>().Name);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericLightweightRootWithTwoTypeParameters()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T1, T2> { string Name { get; } }
                           class Service<T1, T2> : IService<T1, T2>
                           {
                               public string Name { get; } = $"{typeof(T1).Name}:{typeof(T2).Name}";
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT1, TT2>>().To<Service<TT1, TT2>>()
                                   .Root<IService<TT1, TT2>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<int, string>().Name);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32:String"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericLightweightRootWithNestedGeneric()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> { int Count { get; } }
                           class Service<T>(IEnumerable<T> values) : IService<T>
                           {
                               public int Count { get; } = values.Count();
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IEnumerable<TT>>().To(_ => new[] { default(TT), default(TT) })
                                   .Bind<IService<TT>>().To<Service<TT>>()
                                   .Root<IService<TT>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<int>().Count);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericLightweightRootWithClassConstraint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> { string Name { get; } }
                           class Service<T> : IService<T> { public string Name { get; } = typeof(T).Name; }
                           class RefType { }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT>>().To<Service<TT>>()
                                   .Root<IService<TT>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<RefType>().Name);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["RefType"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericLightweightRootWithStructConstraint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> { string Name { get; } }
                           class Service<T> : IService<T> { public string Name { get; } = typeof(T).Name; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT>>().To<Service<TT>>()
                                   .Root<IService<TT>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<int>().Name);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericLightweightRootWithNewConstraint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> { int Size { get; } }
                           class Service<T> : IService<T> { public int Size { get; } = 1; }
                           class RefType { }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT>>().To<Service<TT>>()
                                   .Root<IService<TT>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<RefType>().Size);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericLightweightRootWithArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> { string Value { get; } }
                           class Service<T> : IService<T> { public string Value { get; } = typeof(T).Name; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT>>().To<Service<TT>>()
                                   .Root<IService<TT>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<string>().Value);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["String"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericLightweightRootWithTwoArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> { string Value { get; } }
                           class Service<T> : IService<T>
                           {
                               public string Value { get; } = typeof(T).Name;
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT>>().To<Service<TT>>()
                                   .Root<IService<TT>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<int>().Value);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericLightweightRootWithDifferentMarkersAndArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> { string Value { get; } }
                           class Service<T> : IService<T>
                           {
                               public string Value { get; } = typeof(T).Name;
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT1>>().To<Service<TT1>>()
                                   .Root<IService<TT1>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<int>().Value);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericLightweightRootWithTwoTypeParametersAndTwoArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T1, T2> { string Value { get; } }
                           class Service<T1, T2> : IService<T1, T2>
                           {
                               public string Value { get; } = $"{typeof(T1).Name}:{typeof(T2).Name}";
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT1, TT2>>().To<Service<TT1, TT2>>()
                                   .Root<IService<TT1, TT2>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<int, long>().Value);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32:Int64"], result);
    }

    [Fact]
    public async Task ShouldSupportStaticLightweightRootWithArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { int Id { get; } }
                           class Service : IService { public int Id { get; } = 77; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("GetService", kind: RootKinds.Static | RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   Console.WriteLine(Composition.GetService.Id);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["77"], result);
    }

    [Fact]
    public async Task ShouldSupportStaticGenericLightweightRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> { string Name { get; } }
                           class Service<T> : IService<T> { public string Name { get; } = typeof(T).Name; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT>>().To<Service<TT>>()
                                   .Root<IService<TT>>("GetService", kind: RootKinds.Static | RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   Console.WriteLine(Composition.GetService<decimal>().Name);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Decimal"], result);
    }

    [Fact]
    public async Task ShouldSupportExposedLightweightRootWithArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { int Id { get; } }
                           class Service(int id) : IService { public int Id { get; } = id; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("GetService", kind: RootKinds.Exposed | RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService(6).Id);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["6"], result);
    }

    [Fact]
    public async Task ShouldSupportExposedGenericLightweightRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> { string Name { get; } }
                           class Service<T> : IService<T> { public string Name { get; } = typeof(T).Name; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT>>().To<Service<TT>>()
                                   .Root<IService<TT>>("GetService", kind: RootKinds.Exposed | RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.GetService<Guid>().Name);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Guid"], result);
    }

    [Fact]
    public async Task ShouldSupportPublicLightweightRootWithArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           public interface IService { int Id { get; } }
                           public class Service(int id) : IService { public int Id { get; } = id; }

                           public partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Public | RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Create(4).Id);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["4"], result);
    }

    [Fact]
    public async Task ShouldSupportInternalLightweightRootWithArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { int Id { get; } }
                           class Service(int id) : IService { public int Id { get; } = id; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Internal | RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Create(11).Id);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["11"], result);
    }

    [Fact]
    public async Task ShouldSupportPrivateLightweightRootWithArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { int Id { get; } }
                           class Service(int id) : IService { public int Id { get; } = id; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Private | RootKinds.Light);

                               public int UsePrivate(int id) => Create(id).Id;
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.UsePrivate(12));
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["12"], result);
    }

    [Fact]
    public async Task ShouldSupportProtectedLightweightRootWithArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { int Id { get; } }
                           class Service(int id) : IService { public int Id { get; } = id; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Protected | RootKinds.Light);
                           }

                           class DerivedComposition : Composition
                           {
                               public int UseProtected(int id) => Create(id).Id;
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new DerivedComposition();
                                   Console.WriteLine(composition.UseProtected(13));
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["13"], result);
    }

    [Fact]
    public async Task ShouldSupportLightweightRootWithMaximumFuncArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { int Sum { get; } }
                           class Service(int a1, int a2) : IService
                           {
                               public int Sum { get; } = a1 + a2;
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("a1").RootArg<int>("a2")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Create(1,1).Sum);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldFailWhenCallingLightweightRootWithWrongArgumentType()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;

                           namespace Sample;

                           interface IService { }
                           class Service(int id) : IService { }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   var service = composition.Create("wrong");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview, CheckCompilationErrors: false));

        // Then
        result.Success.ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldFailWhenCallingLightweightRootWithoutRequiredArgument()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;

                           namespace Sample;

                           interface IService { }
                           class Service(int id) : IService { }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   var service = composition.Create();
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview, CheckCompilationErrors: false));

        // Then
        result.Success.ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldFailWhenGenericConstraintIsViolatedForLightweightRoot()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;

                           namespace Sample;

                           interface IService<T> where T : class { }
                           class Service<T> : IService<T> where T : class { }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .Bind<IService<TT>>().To<Service<TT>>()
                                   .Root<IService<TT>>("GetService", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   var service = composition.GetService<int>();
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview, CheckCompilationErrors: false));

        // Then
        result.Success.ShouldBeFalse(result);
    }
}
