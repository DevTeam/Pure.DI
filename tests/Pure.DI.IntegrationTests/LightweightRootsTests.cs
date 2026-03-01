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
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 1 Abc" ], result);
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
                           class Service : IService
                           {
                           	private string _name;
                           	public Service(string name)
                           	{
                           		_name = name;
                           	}
                           	public string Name { get { return _name; } }
                           }

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
                           class Service : IService
                           {
                           	private int _id;
                           	public Service(int id)
                           	{
                           		_id = id;
                           	}
                           	public int Id { get { return _id; } }
                           }

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

                           class User
                           {
                           	private string _name;
                           	public User(string name)
                           	{
                           		_name = name;
                           	}
                           	public string Name { get { return _name; } }
                           }
                           interface IService { string Name { get; } }
                           class Service : IService
                           {
                           	private User _user;
                           	public Service(User user)
                           	{
                           		_user = user;
                           	}
                           	public string Name { get { return _user.Name; } }
                           }

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
                           class Service : IService
                           {
                           	private int _id;
                           	private string _name;
                           	public Service(int id, string name)
                           	{
                           		_id = id;
                           		_name = name;
                           	}
                           	public string Value { get { return $"{_id}:{_name}"; } }
                           }

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
                           class Service : IService
                           {
                           	private int _id;
                           	private string _name;
                           	public Service(int id, string name)
                           	{
                           		_id = id;
                           		_name = name;
                           	}
                           	public string Value { get { return $"{_id}:{_name}"; } }
                           }

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
                           class Service : IService
                           {
                           	private string _token;
                           	public Service([Tag("token")] string token)
                           	{
                           		_token = token;
                           	}
                           	public string Token { get { return _token; } }
                           }

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
                           class Service : IService
                           {
                           	private int _id;
                           	private string _name;
                           	public Service([Tag("a")] int id, [Tag("b")] string name)
                           	{
                           		_id = id;
                           		_name = name;
                           	}
                           	public string Value { get { return $"{_id}:{_name}"; } }
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
        result.StdOut.ShouldBe(["Int32"]);
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
                           class Service<T> : IService<T>
                           {
                           	private IEnumerable<T> _values;
                           	public Service(IEnumerable<T> values)
                           	{
                           		_values = values;
                           	}
                           	public int Count { get { return _values.Count(); } }
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
                           class Service : IService
                           {
                           	private int _id;
                           	public Service(int id)
                           	{
                           		_id = id;
                           	}
                           	public int Id { get { return _id; } }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("id")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("GetService", kind: RootKinds.Static | RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   Console.WriteLine(Composition.GetService(77).Id);
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
                           class Service : IService
                           {
                           	private int _id;
                           	public Service(int id)
                           	{
                           		_id = id;
                           	}
                           	public int Id { get { return _id; } }
                           }

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
                           public class Service : IService
                           {
                           	private int _id;
                           	public Service(int id)
                           	{
                           		_id = id;
                           	}
                           	public int Id { get { return _id; } }
                           }

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
                           class Service : IService
                           {
                           	private int _id;
                           	public Service(int id)
                           	{
                           		_id = id;
                           	}
                           	public int Id { get { return _id; } }
                           }

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
                           class Service : IService
                           {
                           	private int _id;
                           	public Service(int id)
                           	{
                           		_id = id;
                           	}
                           	public int Id { get { return _id; } }
                           }

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
                           class Service : IService
                           {
                           	private int _id;
                           	public Service(int id)
                           	{
                           		_id = id;
                           	}
                           	public int Id { get { return _id; } }
                           }

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
                           class Service : IService
                           {
                           	private int _a1, _a2, _a3, _a4, _a5, _a6, _a7, _a8, _a9, _a10, _a11, _a12, _a13, _a14, _a15, _a16;
                           	public Service(
                           		[Tag("a1")] int a1,
                           		[Tag("a2")] int a2,
                           		[Tag("a3")] int a3,
                           		[Tag("a4")] int a4,
                           		[Tag("a5")] int a5,
                           		[Tag("a6")] int a6,
                           		[Tag("a7")] int a7,
                           		[Tag("a8")] int a8,
                           		[Tag("a9")] int a9,
                           		[Tag("a10")] int a10,
                           		[Tag("a11")] int a11,
                           		[Tag("a12")] int a12,
                           		[Tag("a13")] int a13,
                           		[Tag("a14")] int a14,
                           		[Tag("a15")] int a15,
                           		[Tag("a16")] int a16)
                           	{
                           		_a1 = a1; _a2 = a2; _a3 = a3; _a4 = a4;
                           		_a5 = a5; _a6 = a6; _a7 = a7; _a8 = a8;
                           		_a9 = a9; _a10 = a10; _a11 = a11; _a12 = a12;
                           		_a13 = a13; _a14 = a14; _a15 = a15; _a16 = a16;
                           	}
                           	public int Sum { get { return _a1 + _a2 + _a3 + _a4 + _a5 + _a6 + _a7 + _a8 + _a9 + _a10 + _a11 + _a12 + _a13 + _a14 + _a15 + _a16; } }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("a1", "a1")
                                   .RootArg<int>("a2", "a2")
                                   .RootArg<int>("a3", "a3")
                                   .RootArg<int>("a4", "a4")
                                   .RootArg<int>("a5", "a5")
                                   .RootArg<int>("a6", "a6")
                                   .RootArg<int>("a7", "a7")
                                   .RootArg<int>("a8", "a8")
                                   .RootArg<int>("a9", "a9")
                                   .RootArg<int>("a10", "a10")
                                   .RootArg<int>("a11", "a11")
                                   .RootArg<int>("a12", "a12")
                                   .RootArg<int>("a13", "a13")
                                   .RootArg<int>("a14", "a14")
                                   .RootArg<int>("a15", "a15")
                                   .RootArg<int>("a16", "a16")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16).Sum);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["136"], result);
    }


    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(30)]
    public async Task ShouldSupportLightweightRootWithTaggedIntArgumentScenarios(int value)
    {
        // Given

        // When
        var result = await $$"""
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                               int Value { get; }
                               int DoubleValue { get; }
                           }

                           class Service : IService
                           {
                           	private int _value;
                           	public Service([Tag("input")] int value)
                           	{
                           		_value = value;
                           	}
                           	public int Value { get { return _value; } }

                           	public int DoubleValue { get { return _value * 2; } }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup(nameof(Composition))
                                   .Hint(Hint.Resolve, "Off")
                                   .RootArg<int>("value", "input")
                                   .Bind<IService>().To<Service>()
                                   .Root<IService>("Create", kind: RootKinds.Light);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   var service = composition.Create({{value}});
                                   Console.WriteLine($"{service.Value}:{service.DoubleValue}");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([$"{value}:{value * 2}"], result);
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
                           class Service : IService
                           {
                           	private int _id;
                           	public Service(int id)
                           	{
                           		_id = id;
                           	}
                           }

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
                           class Service : IService
                           {
                           	private int _id;
                           	public Service(int id)
                           	{
                           		_id = id;
                           	}
                           }

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
