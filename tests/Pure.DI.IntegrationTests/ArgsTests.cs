namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
/// Tests related to the composition arguments feature, allowing to pass arguments to the composition root.
/// </summary>
public class ArgsTests
{

    [Theory]
    [InlineData("\"my_id\"", "id_int_my_id")]
    [InlineData("null", "id_int_")]
    [InlineData("\"\"", "id_int_")]
    [InlineData("\"1my_id\"", "id_int_1my_id")]
    [InlineData("\"my   Id\"", "id_int_myId")]
    [InlineData("\"my.Id\"", "id_int_myId")]
    [InlineData("\"my<Id\"", "id_int_my_Id")]
    [InlineData("\"my>Id\"", "id_int_myId")]
    [InlineData("\"my[Id\"", "id_int_my_Id")]
    [InlineData("\"my]Id\"", "id_int_myId")]
    [InlineData("\"my`Id\"", "id_int_myId")]
    public async Task ShouldSupportRootArgWithNameTemplate(string tag, string argName)
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
                              
                              class Dependency2
                              {
                                   public Dependency2([Tag(#tag#)] int id) {}
                              }
                           
                              interface IService
                              {
                                  IDependency Dep { get; }
                           
                                  string Name { get; }
                              }
                           
                              class Service: IService
                              {
                                  public Service(IDependency dep, [Tag(#tag#)] int id, string name)
                                  {
                                      Dep = dep;
                                      Name = name + "_" + id;
                                  }
                           
                                  public IDependency Dep { get; }
                           
                                  public string Name { get; private set; }
                              }
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      // ToString = On
                                      DI.Setup("Composition")
                                          .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                          .Bind<IService>().To<Service>()
                                          .Root<Dependency2>()
                                          .RootArg<string>("serviceName_{type}")
                                          .RootArg<int>("id_{type}_{tag}", #tag#)
                                          .Root<IService>("GetService");
                                  }
                              }
                           
                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.GetService(serviceName_string: "Some Name", #argName#: 99).Name);
                                  }
                              }
                           }
                           """.Replace("#tag#", tag).Replace("#argName#", argName).RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningRootArgInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<Dependency2>()").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningRootArgInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IService>(\"GetService\")").ShouldBe(1, result);
        result.StdOut.ShouldBe(["Some Name_99"], result);
    }

    [Fact]
    public async Task ShouldNotShowWarningAboutRootArgWhenResolveMethodsAreNotGenerated()
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
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService
                               {
                                   public Service(IDependency dep, string name)
                                   {
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.Resolve, "Off")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .RootArg<string>("serviceName")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service("Some Name").Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name"], result);
    }

    [Fact]
    public async Task ShouldSupportArg()
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
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep, string name)
                                   { 
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("serviceName")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name"], result);
    }
    [Fact]
    public async Task ShouldSupportArgAsRoot()
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
                                           .Arg<string>("serviceName")
                                           .Root<string>("ServiceName");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.ServiceName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name"], result);
    }

    [Fact]
    public async Task ShouldSupportArgWhenFewConsumers()
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
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep, string name)
                                   { 
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                               
                               class Service2 
                               {
                                   public Service2(string name)
                                   { 
                                       Name = name;
                                   }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("serviceName")
                                           .Root<IService>("Service")
                                           .Root<Service2>("Service2");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.Service.Name);
                                       Console.WriteLine(composition.Service2.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name", "Some Name"], result);
    }

    [Fact]
    public async Task ShouldSupportArgWhenFewDeps()
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
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep, string name, string name2)
                                   { 
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("serviceName")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name"], result);
    }

    [Fact]
    public async Task ShouldSupportArgWithNameTemplate()
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
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep, string name)
                                   { 
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("serviceName_{type}")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(serviceName_string: "Some Name");
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name"], result);
    }

    [Fact]
    public async Task ShouldSupportRootArg()
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
                              
                              class Dependency2
                              {
                                   public Dependency2(int id) {}
                              }
                           
                              interface IService
                              {
                                  IDependency Dep { get; }
                           
                                  string Name { get; }
                              }
                           
                              class Service: IService
                              {
                                  public Service(IDependency dep, int id, string name)
                                  {
                                      Dep = dep;
                                      Name = name + "_" + id;
                                  }
                           
                                  public IDependency Dep { get; }
                           
                                  public string Name { get; private set; }
                              }
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      // ToString = On
                                      DI.Setup("Composition")
                                          .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                          .Bind<IService>().To<Service>()
                                          .Root<Dependency2>()
                                          .RootArg<string>("serviceName")
                                          .RootArg<int>("id")
                                          .Root<IService>("GetService");
                                  }
                              }
                           
                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.GetService("Some Name", 99).Name);
                                  }
                              }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningRootArgInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IService>(\"GetService\")").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningRootArgInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<Dependency2>()").ShouldBe(1, result);
        result.StdOut.ShouldBe(["Some Name_99"], result);
    }

#if ROSLYN5_6_OR_GREATER
    [Fact]
    public async Task ShouldSupportScopedReadOnlySpanRootArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                              class Parser
                              {
                                  private int _length;

                                  [Ordinal]
                                  public void Initialize(ReadOnlySpan<char> text)
                                  {
                                      _length = text.Length;
                                  }

                                  public int Length => _length;
                              }

                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      // Resolve = Off
                                      DI.Setup("Composition")
                                          .RootArg<ReadOnlySpan<char>>("text")
                                          .Root<Parser>("Parse");
                                  }
                              }

                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Parse("Hello".AsSpan()).Length);
                                  }
                              }
                           }
                           """.RunAsync(new Options(
                               LanguageVersion.Preview,
                               PreprocessorSymbols: ["NET", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER"]));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["5"], result);
        result.GeneratedCode.ShouldContain("public global::Sample.Parser Parse(scoped System.ReadOnlySpan<char> text)");
    }

    [Fact]
    public async Task ShouldSupportAllowsRefStructGenericWithImmediateMethodUse()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                              class Parser<T>
                                  where T : allows ref struct
                              {
                                  private bool _initialized;

                                  [Ordinal]
                                  public void Initialize(T text)
                                  {
                                      _initialized = true;
                                  }

                                  public bool Initialized => _initialized;
                              }

                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      // Resolve = Off
                                      DI.Setup("Composition")
                                          .RootArg<ReadOnlySpan<char>>("text")
                                          .Root<Parser<ReadOnlySpan<char>>>("Parse");
                                  }
                              }

                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Parse("Hello".AsSpan()).Initialized);
                                  }
                              }
                           }
                           """.RunAsync(new Options(
                               LanguageVersion.Preview,
                               PreprocessorSymbols: ["NET", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER"]));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSeveralAllowsRefStructGenericRootArgs()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                              class Parser<TText, TPattern>
                                  where TText : allows ref struct
                                  where TPattern : allows ref struct
                              {
                                  private int _length;

                                  [Ordinal]
                                  public void Initialize(TText text, TPattern pattern)
                                  {
                                      _ = text;
                                      _ = pattern;
                                      _length = 2;
                                  }

                                  public int Length => _length;
                              }

                              partial class Composition<TText, TPattern>
                                  where TText : allows ref struct
                                  where TPattern : allows ref struct
                              {
                                  static void Setup() =>
                                      // Resolve = Off
                                      DI.Setup()
                                          .RootArg<TText>("text")
                                          .RootArg<TPattern>("pattern")
                                          .Root<Parser<TText, TPattern>>("Parse");
                              }

                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var composition = new Composition<ReadOnlySpan<char>, ReadOnlySpan<char>>();
                                      Console.WriteLine(composition.Parse("Hello".AsSpan(), "ell".AsSpan()).Length);
                                  }
                              }
                           }
                           """.RunAsync(new Options(
                               LanguageVersion.Preview,
                               PreprocessorSymbols: ["NET", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER"]));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
        result.GeneratedCode.ShouldContain("Parse(scoped TText text, scoped TPattern pattern)");
    }

    [Fact]
    public async Task ShouldUseScopedOnlyForMaybeRefStructRootArgs()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                              class Parser<T>
                                  where T : allows ref struct
                              {
                                  private string _value = "";

                                  [Ordinal]
                                  public void Initialize(T text, string name, int count)
                                  {
                                      _ = text;
                                      _value = $"{name}:{count}";
                                  }

                                  public string Value => _value;
                              }

                              partial class Composition<T>
                                  where T : allows ref struct
                              {
                                  static void Setup() =>
                                      // Resolve = Off
                                      DI.Setup()
                                          .RootArg<T>("text")
                                          .RootArg<string>("name")
                                          .RootArg<int>("count")
                                          .Root<Parser<T>>("Parse");
                              }

                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var composition = new Composition<ReadOnlySpan<char>>();
                                      Console.WriteLine(composition.Parse("Hello".AsSpan(), "name", 3).Value);
                                  }
                              }
                           }
                           """.RunAsync(new Options(
                               LanguageVersion.Preview,
                               PreprocessorSymbols: ["NET", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER"]));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["name:3"], result);
        result.GeneratedCode.ShouldContain("Parse(scoped T text, string name, int count)");
        result.GeneratedCode.ShouldNotContain("scoped int count");
        result.GeneratedCode.ShouldNotContain("scoped string name");
    }

    [Fact]
    public async Task ShouldNotUseScopedForHeapSafeGenericRootArgWrapper()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                              sealed class Wrapper<T>
                                  where T : allows ref struct
                              {
                                  public Wrapper(int value) => Value = value;

                                  public int Value { get; }
                              }

                              class Parser<T>
                                  where T : allows ref struct
                              {
                                  private int _value;

                                  [Ordinal]
                                  public void Initialize(Wrapper<T> wrapper)
                                  {
                                      _value = wrapper.Value;
                                  }

                                  public int Value => _value;
                              }

                              partial class Composition<T>
                                  where T : allows ref struct
                              {
                                  static void Setup() =>
                                      // Resolve = Off
                                      DI.Setup()
                                          .RootArg<Wrapper<T>>("wrapper")
                                          .Root<Parser<T>>("Parse");
                              }

                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var composition = new Composition<ReadOnlySpan<char>>();
                                      Console.WriteLine(composition.Parse(new Wrapper<ReadOnlySpan<char>>(7)).Value);
                                  }
                              }
                           }
                           """.RunAsync(new Options(
                               LanguageVersion.Preview,
                               PreprocessorSymbols: ["NET", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER"]));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["7"], result);
        result.GeneratedCode.ShouldContain("Parse(Sample.Wrapper<T> wrapper)");
        result.GeneratedCode.ShouldNotContain("Parse(scoped Sample.Wrapper<T> wrapper)");
    }
#endif

    [Fact]
    public async Task ShouldSupportSeveralArgs()
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
                                   int _id;
                           
                                   public Dependency(int id)
                                   { 
                                       _id = id;
                                   }
                           
                                   public override string ToString() => _id.ToString();
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   int _id;
                                   string _name;
                           
                                   public Service(IDependency dep, [Tag(99)] int id, string name)
                                   { 
                                       Dep = dep;
                                       _id = id;
                                       _name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name => $"{_name} {_id} {Dep}";
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("serviceName")
                                           .Arg<int>("id", 99)
                                           .Arg<int>("depId")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name", 37, 56);
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name 37 56"], result);
    }

    [Fact]
    public async Task ShouldSupportArgInFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService { string Name { get; } }

                               class Service : IService
                               {
                                   public Service(string name) => Name = name;
                                   public string Name { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Arg<string>("name", "nameTag")
                                           .Bind<IService>().To(ctx => {
                                               ctx.Inject<string>("nameTag", out var name);
                                               return new Service(name);
                                           })
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Arg Value");
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Arg Value"], result);
    }

    [Fact]
    public async Task ShouldSupportArgWithDefaultValue()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService { string Name { get; } }

                               class Service : IService
                               {
                                   public Service(string name = "Default") => Name = name;
                                   public string Name { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Arg<string>("name")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Arg Value");
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Arg Value"], result);
    }

    [Fact]
    public async Task ShouldSupportArgInGenericComposition()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService<T> { T Value { get; } }

                               class Service<T> : IService<T>
                               {
                                   public Service(T value) => Value = value;
                                   public T Value { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Arg<int>("val")
                                           .Bind<IService<int>>().To<Service<int>>()
                                           .Root<IService<int>>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(123);
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["123"], result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldSupportTagsAsArray()
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
                                           .Arg<int>("x")
                                           .Arg<int>("y", [1, 2])
                                           .Root<int>("Root")
                                           .Root<int>("Root2", 1)
                                           .Root<int>("Root3", 2);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(-1, 1);
                                       Console.WriteLine(composition.Root);
                                       Console.WriteLine(composition.Root2);
                                       Console.WriteLine(composition.Root3);
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.Preview });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["-1", "1", "1"], result);
    }
#endif
    [Fact]
    public async Task ShouldSupportMultipleArgsOfSameTypeWithTags()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService 
                               {
                                   public Service([Tag("a")] string arg1, [Tag("b")] string arg2) 
                                   {
                                       Console.WriteLine($"{arg1} {arg2}");
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Arg<string>("arg1", "a")
                                           .Arg<string>("arg2", "b")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Hello", "World");
                                       var service = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Hello World"], result);
    }

    [Fact]
    public async Task ShouldSupportArgAsRootWithTag()
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
                                           .Arg<int>("id", "myId")
                                           .Root<int>("GetId", "myId");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(99);
                                       Console.WriteLine(composition.GetId);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["99"], result);
    }

    [Fact]
    public async Task ShouldSupportTypeAliasInArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           
                           namespace Abc
                           {
                              public class User
                              {
                                  public User(int id, string name)
                                  {
                                      Id = id;
                                      Name = name;
                                  }

                                  public int Id { get; }
                                  public string Name { get; }
                              }
                           }

                           namespace Sample
                           {
                              using UserAlias = Abc.User;
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      DI.Setup("Composition")
                                          .Arg<UserAlias>("user")
                                          .Root<UserAlias>("User");
                                  }
                              }
                           
                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var user = new UserAlias(1, "Abc");
                                      var composition = new Composition(user);
                                      Console.WriteLine(composition.User.Name);
                                  }
                              }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportTypeAliasInRootArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           
                           namespace Abc
                           {
                              public class Config
                              {
                                  public string Value { get; set; } = "";
                              }
                           }

                           namespace Sample
                           {
                              using ConfigAlias = Abc.Config;
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      DI.Setup("Composition")
                                          .Hint(Hint.Resolve, "Off")
                                          .RootArg<ConfigAlias>("config")
                                          .Bind<string>().To(ctx => {
                                              ctx.Inject(out ConfigAlias c);
                                              return c.Value;
                                          })
                                          .Root<string>("Value");
                                  }
                              }
                              
                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var config = new ConfigAlias { Value = "Abc" };
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Value(config));
                                  }
                              }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Abc"], result);
    }
}
