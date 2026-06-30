namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
/// Tests related to nullable reference type support in dependency contracts and generated code.
/// </summary>
public class NullableReferenceTypesTests
{
    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    [InlineData(NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Warnings)]
    public async Task ShouldSupportNullableRootAndCompositionArguments(NullableContextOptions nullableContextOptions)
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample
                           {
                               interface IService
                               {
                                   string? ConnectionString { get; }

                                   string? AppName { get; }
                               }

                               class Service: IService
                               {
                                   public Service([Tag("connection")] string? connectionString, [Tag("app")] string? appName)
                                   {
                                       ConnectionString = connectionString;
                                       AppName = appName;
                                   }

                                   public string? ConnectionString { get; }

                                   public string? AppName { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.Resolve, "Off")
                                           .Bind<IService>().To<Service>()
                                           .Arg<string?>("appName", "app")
                                           .RootArg<string?>("connectionString", "connection")
                                           .Root<IService>("CreateService");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(appName: null);
                                       var service = composition.CreateService(connectionString: null);
                                       Console.WriteLine(service.ConnectionString is null);
                                       Console.WriteLine(service.AppName is null);
                                   }
                               }
                           }
                           """.RunAsync(new Options { NullableContextOptions = nullableContextOptions });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
        result.GeneratedCode.Contains("public Composition(string? appName)", StringComparison.Ordinal).ShouldBeTrue(result);
        result.GeneratedCode.Contains("CreateService(string? connectionString)", StringComparison.Ordinal).ShouldBeTrue(result);
        result.GeneratedCode.Contains("if (connectionString == null)", StringComparison.Ordinal).ShouldBeFalse(result);
        result.GeneratedCode.Contains("throw new global::System.ArgumentNullException(nameof(appName))", StringComparison.Ordinal).ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldSupportNullableConstructorDependenciesAndNullableGenerics()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency
                               {
                               }

                               class Dependency: IDependency
                               {
                               }

                               interface IService
                               {
                                   IDependency? OptionalDependency { get; }

                                   IReadOnlyList<IDependency?> Dependencies { get; }
                               }

                               class Service: IService
                               {
                                   public Service(IDependency? optionalDependency, Func<IDependency?> dependencyFactory, IEnumerable<IDependency?> dependencies)
                                   {
                                       OptionalDependency = optionalDependency;
                                       var result = new List<IDependency?> { dependencyFactory() };
                                       result.AddRange(dependencies);
                                       Dependencies = result;
                                   }

                                   public IDependency? OptionalDependency { get; }

                                   public IReadOnlyList<IDependency?> Dependencies { get; }
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
                                       var service = new Composition().Service;
                                       Console.WriteLine(service.OptionalDependency is not null);
                                       Console.WriteLine(service.Dependencies.Count > 0);
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
        result.GeneratedCode.Contains("System.Func<global::Sample.IDependency?>", StringComparison.Ordinal).ShouldBeTrue(result);
        result.GeneratedCode.Contains("System.Collections.Generic.IEnumerable<global::Sample.IDependency?>", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldShowNullableTypesInMermaidDiagram()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency
                               {
                               }

                               class Dependency: IDependency
                               {
                               }

                               class Service
                               {
                                   public Service(IDependency? dependency)
                                   {
                                       Dependency = dependency;
                                   }

                                   public IDependency? Dependency { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // ToString = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<Service>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var diagram = new Composition().ToString();
                                       System.Console.WriteLine(diagram.Contains("IDependencyɁ"));
                                       System.Console.WriteLine(diagram.Contains("IDependency?"));
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableFieldInjection()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               [Dependency] public IDependency? Dependency;
                               public bool IsReady => Dependency is not null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service?.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullablePropertyInjection()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               [Dependency] public IDependency? Dependency { get; set; }
                               public bool IsReady => Dependency is not null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service?.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableMethodInjection()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               private IDependency? _dependency;
                               [Dependency] public void Init(IDependency? dependency) => _dependency = dependency;
                               public bool IsReady => _dependency is not null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service?.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableRequiredPropertyInjection()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               [Dependency]
                               public IDependency? Dependency { get; init; }
                               public bool IsReady => Dependency is not null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service?.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableFuncResult()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               private readonly Func<IDependency?> _factory;

                               public Service(Func<IDependency?> factory) => _factory = factory;

                               public bool IsReady => _factory() is not null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service?.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("System.Func<global::Sample.IDependency?>", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportNullableFuncArgumentAndResult()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency { string? Name { get; } }
                           class Dependency: IDependency
                           {
                               public Dependency(string? name) => Name = name;

                               public string? Name { get; }
                           }
                           class Service
                           {
                               private readonly Func<string?, IDependency?> _factory;

                               public Service(Func<string?, IDependency?> factory) => _factory = factory;

                               public bool IsReady => _factory(null)?.Name is null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service?.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("System.Func<string?, global::Sample.IDependency?>", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportNullableLazy()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               private readonly Lazy<IDependency?> _dependency;

                               public Service(Lazy<IDependency?> dependency) => _dependency = dependency;

                               public bool IsReady => _dependency.Value is not null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableEnumerable()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               private readonly IEnumerable<IDependency?> _dependencies;

                               public Service(IEnumerable<IDependency?> dependencies) => _dependencies = dependencies;

                               public bool IsReady => _dependencies.Any(i => i is not null);
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("System.Collections.Generic.IEnumerable<global::Sample.IDependency?>", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportNullableReadOnlyList()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               private readonly IReadOnlyList<IDependency?> _dependencies;

                               public Service(IReadOnlyList<IDependency?> dependencies) => _dependencies = dependencies;

                               public bool IsReady => _dependencies.Any(i => i is not null);
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("System.Collections.ObjectModel.ReadOnlyCollection<global::Sample.IDependency?>", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportNullableArray()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               private readonly IDependency?[] _dependencies;

                               public Service(IDependency?[] dependencies) => _dependencies = dependencies;

                               public bool IsReady => _dependencies.Any(i => i is not null);
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseNonNullableBindingForNullableRootContract()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { bool IsReady { get; } }
                           class Service: IService { public bool IsReady => true; }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService?>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service?.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("#nullable enable annotations", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportNullableRootArg()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? name) => IsReady = name is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Hint(Hint.Resolve, "Off")
                                       .RootArg<string?>("name")
                                       .Root<Service>("Create");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Create(null).IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("Create(string? name)", StringComparison.Ordinal).ShouldBeTrue(result);
        result.GeneratedCode.Contains("throw new global::System.ArgumentNullException(nameof(name))", StringComparison.Ordinal).ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldSupportNullableCompositionArg()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? name) => IsReady = name is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Arg<string?>("name")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition(null).Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("public Composition(string? name)", StringComparison.Ordinal).ShouldBeTrue(result);
        result.GeneratedCode.Contains("throw new global::System.ArgumentNullException(nameof(name))", StringComparison.Ordinal).ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldSupportNullableTaggedPrimitiveArg()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service([Tag("name")] string? name) => IsReady = name is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Arg<string?>("name", "name")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition(null).Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableAndNonNullableTaggedPrimitiveArgs()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service([Tag("optional")] string? optional, [Tag("required")] string required) =>
                                   IsReady = optional is null && required == "abc";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Arg<string?>("optional", "optional")
                                       .Arg<string>("required", "required")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition(null, "abc").Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDistinguishNullableAndNonNullableReferenceTypeBindings()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value, string? optionalValue) =>
                                   IsReady = value == "required" && optionalValue is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldWarnWhenNullableReferenceTypeBindingIsNotUsed()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value) => IsReady = value == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningBindingNotUsed && i.Locations.FirstOrDefault().GetSource() == "To(_ => (string?)null)").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldWarnWhenNonNullableReferenceTypeBindingIsNotUsed()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningBindingNotUsed && i.Locations.FirstOrDefault().GetSource() == "To(_ => \"required\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldNotUseNullableReferenceTypeBindingForNonNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => "optional")
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseNonNullableReferenceTypeBindingForNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseExactFactoryBindingForNullableAndNonNullableReferenceTypes()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value, string? optionalValue) =>
                                   IsReady = value == "required" && optionalValue == "optional";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(ctx => "required")
                                       .Bind<string?>().To(ctx => "optional")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseExactContextInjectTypeForNullableAndNonNullableReferenceTypes()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value, string? optionalValue) =>
                                   IsReady = value == "required" && optionalValue is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Inject<string>(out var value);
                                           ctx.Inject<string?>(out var optionalValue);
                                           return new Service(value, optionalValue);
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseNullableReferenceTypeBindingForNonNullableContextInject()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => "optional")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Inject<string>(out var value);
                                           return new Service(value);
                                       })
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseNullableGenericBindingForNullableGenericDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T> { T Value { get; } }
                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }
                           class Service
                           {
                               public Service(IBox<string?> box) => IsReady = box.Value is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseNonNullableGenericBindingForNullableGenericDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T> { T Value { get; } }
                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }
                           class Service
                           {
                               public Service(IBox<string?> box) => IsReady = box.Value == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseNullableAndNonNullableReferenceTypeBindingsInBuilder()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               [Dependency] public string Value { get; set; } = "";
                               [Dependency] public string? OptionalValue { get; set; }
                               public bool IsReady => Value == "required" && OptionalValue is null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Builder<Service>("BuildUpService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var service = new Service();
                                   new Composition().BuildUpService(service);
                                   Console.WriteLine(service.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseNonNullableOverrideForNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value == "override";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Override<string>("override");
                                           ctx.Inject(out Service service);
                                           return service;
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseNullableOverrideForNonNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Override<string?>((string?)"override");
                                           ctx.Inject(out Service service);
                                           return service;
                                       })
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseNonNullableLetForNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value == "let";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Let("let");
                                           ctx.Inject(out Service service);
                                           return service;
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseNullableLetForNonNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Let((string?)"let");
                                           ctx.Inject(out Service service);
                                           return service;
                                       })
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseNonNullableCompositionArgForNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value == "arg";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Arg<string>("value")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition("arg").Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseNullableCompositionArgForNonNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Arg<string?>("value")
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseExactNullableAndNonNullableCompositionArgs()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value, string? optionalValue) =>
                                   IsReady = value == "required" && optionalValue is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Arg<string>("value")
                                       .Arg<string?>("optionalValue")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition("required", null).Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseNonNullableRootArgForNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value == "root";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Hint(Hint.Resolve, "Off")
                                       .RootArg<string>("value")
                                       .Root<Service>("CreateService");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().CreateService("root").IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseNullableRootArgForNonNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Hint(Hint.Resolve, "Off")
                                       .RootArg<string?>("value")
                                       .Root<Service>("CreateService");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseTaggedNonNullableBindingForTaggedNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service([Tag("name")] string? value) => IsReady = value == "tagged";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>("name").To(_ => "tagged")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseTaggedNullableBindingForTaggedNonNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service([Tag("name")] string value)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>("name").To(_ => "tagged")
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseExactTaggedNullableAndNonNullableBindings()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service([Tag("name")] string value, [Tag("name")] string? optionalValue) =>
                                   IsReady = value == "required" && optionalValue is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>("name").To(_ => "required")
                                       .Bind<string?>("name").To(_ => (string?)null)
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseTaggedNonNullableBindingForTaggedNullableContextInject()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value == "tagged";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>("name").To(_ => "tagged")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Inject<string?>("name", out var value);
                                           return new Service(value);
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseTaggedNullableBindingForTaggedNonNullableContextInject()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>("name").To(_ => "tagged")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Inject<string>("name", out var value);
                                           return new Service(value);
                                       })
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldSupportNullableRootContract()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }
                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService?>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service is not null);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("IService?", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldNotUseNullableBindingForNonNullableRootContract()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }
                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService?>().To<Service>()
                                       .Root<IService>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldResolveNullableReferenceTypeRoot()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }
                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService?>();
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Resolve<IService?>() is not null);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableReferenceTypeRootWithResolveByRuntimeType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }
                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService?>();
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Resolve(typeof(IService)) is not null);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldWarnWhenNullableAndNonNullableRootsHaveSameRuntimeType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService>("Service")
                                       .Root<IService?>("NullableService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod).ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IService>(\"Service\")").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IService?>(\"NullableService\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldNotWarnWhenNullableAndNonNullableRootsHaveSameRuntimeTypeButResolveIsOff()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Hint(Hint.Resolve, "Off")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService>("Service")
                                       .Root<IService?>("NullableService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod).ShouldBe(0, result);
    }

    [Fact]
    public async Task ShouldWarnWhenTaggedNullableAndNonNullableRootsHaveSameRuntimeType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>("service", "nullable").To<Service>()
                                       .Root<IService>("Service", "service")
                                       .Root<IService?>("NullableService", "nullable");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod).ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IService>(\"Service\", \"service\")").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IService?>(\"NullableService\", \"nullable\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldWarnWhenNullableAndNonNullableGenericRootsHaveSameRuntimeType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                               T Value { get; }
                           }

                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "value")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Root<IBox<string>>("Box")
                                       .Root<IBox<string?>>("NullableBox");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod).ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IBox<string>>(\"Box\")").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IBox<string?>>(\"NullableBox\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldNotWarnWhenOnlyNullableRootHasRuntimeResolveType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService?>("NullableService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod).ShouldBe(0, result);
    }

    [Fact]
    public async Task ShouldInjectNullableEnumerableFromNonNullableBinding()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(IEnumerable<string?> values) => IsReady = values.Single() == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotInjectNonNullableEnumerableFromNullableBinding()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;
                           using System.Collections.Generic;

                           namespace Sample;

                           class Service
                           {
                               public Service(IEnumerable<string> values)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => "optional")
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldInjectNullableArrayFromNonNullableBinding()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string?[] values) => IsReady = values.Single() == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldInjectNullableReadOnlyListFromNonNullableBinding()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(IReadOnlyList<string?> values) => IsReady = values.Single() == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseNonNullableBindingForNullableFunc()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(Func<string?> factory) => IsReady = factory() == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseNullableBindingForNonNullableFunc()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(Func<string> factory)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => "optional")
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseNonNullableBindingForNullableLazy()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(Lazy<string?> lazy) => IsReady = lazy.Value == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseNullableBindingForNonNullableLazy()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(Lazy<string> lazy)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => "optional")
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldSupportNullableContextInjectWithOutVariableType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Inject(out string? value);
                                           return new Service(value);
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseNullableBindingForNonNullableContextInjectWithOutVariableType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => "optional")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Inject(out string value);
                                           return new Service(value);
                                       })
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseAutoBindingForNullableConcreteDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Dependency
                           {
                           }
                           class Service
                           {
                               public Service(Dependency? dependency) => IsReady = dependency is not null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseExplicitDefaultForNullableReferenceTypeWhenBindingIsMissing()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value = null) => IsReady = value is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseExplicitDefaultForNonNullableReferenceTypeWhenBindingIsMissing()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value = "default") => IsReady = value == "default";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableExplicitDefaultValue()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Service
                           {
                               public Service(IDependency? dependency = null) => IsReady = dependency is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableContextInject()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               public Service(IDependency? dependency) => IsReady = dependency is not null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Inject<IDependency?>(out var dependency);
                                           return new Service(dependency);
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableTaggedContextInject()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? name) => IsReady = name == "abc";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>("name").To(_ => "abc")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Inject<string?>("name", out var name);
                                           return new Service(name);
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableBuildUpPropertyInFactory()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               [Dependency] public IDependency? Dependency { get; set; }
                               public bool IsReady => Dependency is not null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<Service>().To(ctx =>
                                       {
                                           var service = new Service();
                                           ctx.BuildUp(service);
                                           return service;
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableGenericContract()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           interface IBox<T> { T Value { get; } }
                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }
                           class Service
                           {
                               public Service(IBox<IDependency?> box) => IsReady = box.Value is not null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableNestedGenericContract()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           interface IBox<T> { T Value { get; } }
                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }
                           class Service
                           {
                               public Service(IBox<IEnumerable<IDependency?>> box) =>
                                   IsReady = box.Value.Any(i => i is not null);

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableTupleElement()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               public Service((IDependency? Dependency, string? Name) value) =>
                                   IsReady = value.Dependency is not null && value.Name == "abc";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<string>().To(_ => "abc")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableAutoBindingRoot()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public bool IsReady => true;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Root<Service?>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service?.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableResolveMethod()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { bool IsReady { get; } }
                           class Service: IService { public bool IsReady => true; }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService?>();
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Resolve<IService?>()!.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableFactoryReturn()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { bool IsReady { get; } }
                           class Service: IService { public bool IsReady => true; }
                           class Consumer
                           {
                               public Consumer(IService? service) => IsReady = service is not null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService?>().To(_ => new Service())
                                       .Root<Consumer>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullablePerBlockLifetime()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               public Service(IDependency? first, IDependency? second) =>
                                   IsReady = first is not null && ReferenceEquals(first, second);

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().As(Lifetime.PerBlock).To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableSingletonLifetime()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               public Service(IDependency? first, IDependency? second) =>
                                   IsReady = first is not null && ReferenceEquals(first, second);

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableScopedLifetime()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               public Service(IDependency? first, IDependency? second) =>
                                   IsReady = first is not null && ReferenceEquals(first, second);

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().As(Lifetime.Scoped).To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableOverride()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? name) => IsReady = name == "abc";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Override<string>("abc");
                                           ctx.Inject(out Service service);
                                           return service;
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldShowNullableConstructorDependencyInGeneratedComments()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}
                           class Service
                           {
                               public Service(IDependency? dependency) => IsReady = dependency is not null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("IDependency?", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportNullableGenericFactoryWithExplicitInject()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency<T> { T Value { get; } }
                           class Dependency<T>: IDependency<T>
                           {
                               public Dependency(T value) => Value = value;

                               public T Value { get; }
                           }
                           class Service
                           {
                               public Service(IDependency<string?> dependency) => IsReady = dependency.Value is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Hint(Hint.Resolve, "Off")
                                       .RootArg<string?>("value")
                                       .Bind<IDependency<TT>>().To(ctx =>
                                       {
                                           ctx.Inject<TT>(out var value);
                                           return new Dependency<TT>(value);
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service(null).IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseExactNullableAndNonNullableBindingsForMethodInjection()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               private string _value = "";
                               private string? _optionalValue = "missing";

                               [Ordinal(1)]
                               public void Initialize(string value, string? optionalValue)
                               {
                                   _value = value;
                                   _optionalValue = optionalValue;
                               }

                               public bool IsReady => _value == "required" && _optionalValue is null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseTaggedNullableBindingForTaggedNullableFieldInjection()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               [Tag("name"), Ordinal(1)]
                               public string? Value;

                               public bool IsReady => Value is null;
                           }

                           class RequiredService
                           {
                               public RequiredService([Tag("name")] string value) => IsReady = value == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>("name").To(_ => "required")
                                       .Bind<string?>("name").To(_ => (string?)null)
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldUseNullableCompositionArgInsteadOfNonNullableBindingForNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService(string value) => IsReady = value == "binding";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Arg<string?>("value")
                                       .Bind<string>().To(_ => "binding")
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition(null);
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldUseNonNullableTaggedRootArgForTaggedNullableDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service([Tag("name")] string? value) => IsReady = value == "root";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Hint(Hint.Resolve, "Off")
                                       .RootArg<string>("value", "name")
                                       .Root<Service>("CreateService");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().CreateService("root").IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseExactNullableArrayBindingWhenNullableAndNonNullableArrayBindingsExist()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string?[] values) => IsReady = values.Length == 1 && values[0] is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService(string[] values) => IsReady = values.Length == 1 && values[0] == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string[]>().To(_ => new[] { "required" })
                                       .Bind<string?[]>().To(_ => new string?[] { null })
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldUseExactNullableEnumerableBindingWhenNullableAndNonNullableEnumerableBindingsExist()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(IEnumerable<string?> values) => IsReady = values.Single() is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService(IEnumerable<string> values) => IsReady = values.Single() == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IEnumerable<string>>().To(_ => new[] { "required" })
                                       .Bind<IEnumerable<string?>>().To(_ => new string?[] { null })
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldUseExactNullableLazyBindingWhenNullableAndNonNullableBindingsExist()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(Lazy<string?> value) => IsReady = value.Value is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService(Lazy<string> value) => IsReady = value.Value == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldUseExactNullableFuncBindingWhenNullableAndNonNullableBindingsExist()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(Func<string?> factory) => IsReady = factory() is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService(Func<string> factory) => IsReady = factory() == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldUseExactNullableOverrideForNullableDependencyWhenNonNullableBindingExists()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService(string value) => IsReady = value == "binding";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "binding")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Override<string?>((string?)null);
                                           ctx.Inject(out Service service);
                                           return service;
                                       })
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldUseExactNullableLetForNullableDependencyWhenNonNullableBindingExists()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService(string value) => IsReady = value == "binding";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "binding")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Let((string?)null);
                                           ctx.Inject(out Service service);
                                           return service;
                                       })
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableGenericContractWithTwoTypeArguments()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IMap<TKey, TValue>
                           {
                               TKey Key { get; }

                               TValue Value { get; }
                           }

                           class Map<TKey, TValue>: IMap<TKey, TValue>
                           {
                               public Map(TKey key, TValue value)
                               {
                                   Key = key;
                                   Value = value;
                               }

                               public TKey Key { get; }

                               public TValue Value { get; }
                           }

                           class Service
                           {
                               public Service(IMap<string, string?> map) =>
                                   IsReady = map.Key == "key" && map.Value is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "key")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind<IMap<TT, TT1>>().To<Map<TT, TT1>>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldWarnWhenTaggedNullableReferenceTypeBindingIsNotUsed()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service([Tag("name")] string value) => IsReady = value == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>("name").To(_ => "required")
                                       .Bind<string?>("name").To(_ => (string?)null)
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningBindingNotUsed && i.Locations.FirstOrDefault().GetSource() == "To(_ => (string?)null)").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseNullableDependencyInOrdinalConstructor()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}

                           class Service
                           {
                               public Service() => IsReady = false;

                               [Ordinal(1)]
                               public Service(IDependency? dependency) => IsReady = dependency is not null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableInitPropertyInjection()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}

                           class Service
                           {
                               [Dependency]
                               public IDependency? Dependency { get; init; }

                               public bool IsReady => Dependency is not null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableTupleRootArg()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency {}
                           class Dependency: IDependency {}

                           class Service
                           {
                               public Service((IDependency? Dependency, string? Name) value) =>
                                   IsReady = value.Dependency is not null && value.Name is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Hint(Hint.Resolve, "Off")
                                       .RootArg<(IDependency? Dependency, string? Name)>("value")
                                       .Root<Service>("CreateService");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().CreateService((new Dependency(), null)).IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseAutoBindingForNullableConcreteDependencyWithNestedDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IInner {}
                           class Inner: IInner {}

                           class Dependency
                           {
                               public Dependency(IInner inner) => Inner = inner;

                               public IInner Inner { get; }
                           }

                           class Service
                           {
                               public Service(Dependency? dependency) => IsReady = dependency?.Inner is not null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IInner>().To<Inner>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseExactNullableAndNonNullableBindingsForBuildUpProperty()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               [Dependency]
                               public string Value { get; set; } = "";

                               [Dependency]
                               public string? OptionalValue { get; set; } = "missing";

                               public bool IsReady => Value == "required" && OptionalValue is null;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Builder<Service>("BuildUpService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var service = new Service();
                                   new Composition().BuildUpService(service);
                                   Console.WriteLine(service.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableGenericContractWithClassConstraint()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                               where T : class?
                           {
                               T Value { get; }
                           }

                           class Box<T>: IBox<T>
                               where T : class?
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }

                           class Service
                           {
                               public Service(IBox<string?> box) => IsReady = box.Value is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseExactOpenGenericBindingForNullableAndNonNullableGenericContracts()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                               T Value { get; }
                           }

                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }

                           class NullableBox<T>: IBox<T?>
                               where T : class?
                           {
                               public NullableBox(T? value) => Value = value;

                               public T? Value { get; }
                           }

                           class Service
                           {
                               public Service(IBox<string> box) => IsReady = box.Value == "required";

                               public bool IsReady { get; }
                           }

                           class OptionalService
                           {
                               public OptionalService(IBox<string?> box) => IsReady = box.Value is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Bind<IBox<TT?>>().To<NullableBox<TT>>()
                                       .Root<Service>("Service")
                                       .Root<OptionalService>("OptionalService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.OptionalService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldUseNonNullableOpenGenericBindingForNullableGenericDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                               T Value { get; }
                           }

                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }

                           class Service
                           {
                               public Service(IBox<string?> box) => IsReady = box.Value == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotUseNullableOpenGenericBindingForNonNullableGenericDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                               T Value { get; }
                           }

                           class NullableBox<T>: IBox<T?>
                               where T : class?
                           {
                               public NullableBox(T? value) => Value = value;

                               public T? Value { get; }
                           }

                           class Service
                           {
                               public Service(IBox<string> box)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => "optional")
                                       .Bind<IBox<TT?>>().To<NullableBox<TT>>()
                                       .Root<Service>("Service");
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldWarnWhenNullableOpenGenericBindingIsNotUsed()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                               T Value { get; }
                           }

                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }

                           class NullableBox<T>: IBox<T?>
                               where T : class?
                           {
                               public NullableBox(T? value) => Value = value;

                               public T? Value { get; }
                           }

                           class Service
                           {
                               public Service(IBox<string> box) => IsReady = box.Value == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Bind<IBox<TT?>>().To<NullableBox<TT>>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningBindingNotUsed && i.Locations.FirstOrDefault().GetSource() == "To<NullableBox<TT>>()").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldUseExactOpenGenericFactoryBindingForNullableAndNonNullableGenericContracts()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                               T Value { get; }
                           }

                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }

                           class NullableBox<T>: IBox<T?>
                               where T : class?
                           {
                               public NullableBox(T? value) => Value = value;

                               public T? Value { get; }
                           }

                           class Service
                           {
                               public Service(IBox<string> box) => IsReady = box.Value == "required";

                               public bool IsReady { get; }
                           }

                           class OptionalService
                           {
                               public OptionalService(IBox<string?> box) => IsReady = box.Value is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind<IBox<TT>>().To(ctx =>
                                       {
                                           ctx.Inject<TT>(out var value);
                                           return new Box<TT>(value);
                                       })
                                       .Bind<IBox<TT?>>().To(ctx =>
                                       {
                                           ctx.Inject<TT?>(out var value);
                                           return new NullableBox<TT>(value);
                                       })
                                       .Root<Service>("Service")
                                       .Root<OptionalService>("OptionalService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.OptionalService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldUseNonNullableOpenGenericFactoryBindingForNullableGenericDependency()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                               T Value { get; }
                           }

                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }

                           class Service
                           {
                               public Service(IBox<string?> box) => IsReady = box.Value == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<IBox<TT>>().To(ctx =>
                                       {
                                           ctx.Inject<TT>(out var value);
                                           return new Box<TT>(value);
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSimplifiedBindingWithNullableGenericInterface()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IRepository<T>
                           {
                               T Value { get; }
                           }

                           class Repository: IRepository<string?>
                           {
                               public Repository(string? value) => Value = value;

                               public string? Value { get; }
                           }

                           class Service
                           {
                               public Service(IRepository<string?> repository) => IsReady = repository.Value is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind().To<Repository>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldResolveTaggedNullableReferenceTypeRootByRuntimeType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>("optional").To<Service>()
                                       .Root<IService?>("OptionalService", "optional");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Resolve(typeof(IService), "optional") is not null);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotWarnWhenNullableAndNonNullableRootsHaveSameRuntimeTypeButUseRootArgs()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class Service: IService
                           {
                               public Service(string name) => Name = name;

                               public string Name { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .RootArg<string>("name")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService>("Service")
                                       .Root<IService?>("NullableService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod).ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningRootArgInResolveMethod).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldNotWarnWhenNullableAndNonNullableGenericMarkerRootsHaveSameRuntimeType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                           }

                           class Box<T>: IBox<T>
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Root<IBox<TT>>("Box")
                                       .Root<IBox<TT?>>("NullableBox");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod).ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningTypeArgInResolveMethod).ShouldBe(2, result);
    }

    [Fact]
    public async Task ShouldWarnWhenNullableArrayRootsHaveSameRuntimeType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService[]>("Services")
                                       .Root<IService[]?>("NullableServices");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod).ShouldBe(2, result);
    }

    [Fact]
    public async Task ShouldWarnWhenNullableArrayElementRootsHaveSameRuntimeType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService[]>("Services")
                                       .Root<IService?[]>("NullableServices");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod).ShouldBe(2, result);
    }

    [Fact]
    public async Task ShouldWarnWhenNestedNullableGenericRootsHaveSameRuntimeType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class Service: IService
                           {
                           }

                           interface IBox<T>
                           {
                               T Value { get; }
                           }

                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Root<IBox<IService>>("Box")
                                       .Root<IBox<IService?>>("NullableBox");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNullableRootInResolveMethod).ShouldBe(2, result);
    }

    [Fact]
    public async Task ShouldSupportNullableTaskAndValueTaskDependencies()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(Task<string?> task, ValueTask<string?> valueTask) =>
                                   IsReady = task.Result == "dependency" && valueTask.Result == "dependency";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "dependency")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableCollectionInterfaces()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(
                                   IReadOnlyCollection<string?> readOnlyCollection,
                                   ICollection<string?> collection,
                                   IList<string?> list) =>
                                   IsReady =
                                       readOnlyCollection.Single() == "dependency"
                                       && collection.Single() == "dependency"
                                       && list.Single() == "dependency";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "dependency")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseExactNullableAndNonNullableBindingsForSimplifiedFactoryParameters()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string value, string? optional) =>
                                   IsReady = value == "required" && optional is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind<Service>().To((string value, string? optional) => new Service(value, optional))
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseTaggedExactNullableOverrideForNullableDependencyWhenNonNullableBindingExists()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service([Tag("name")] string? value) => IsReady = value is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService([Tag("name")] string value) => IsReady = value == "binding";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>("name").To(_ => "binding")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Override<string?>((string?)null, "name");
                                           ctx.Inject(out Service service);
                                           return service;
                                       })
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldUseTaggedExactNullableLetForNullableDependencyWhenNonNullableBindingExists()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service([Tag("name")] string? value) => IsReady = value is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService([Tag("name")] string value) => IsReady = value == "binding";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>("name").To(_ => "binding")
                                       .Bind<Service>().To(ctx =>
                                       {
                                           ctx.Let<string?>((string?)null, "name");
                                           ctx.Inject(out Service service);
                                           return service;
                                       })
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableRootBind()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class Service: IService
                           {
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .RootBind<IService?>().To<Service>();
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Resolve<IService?>() is Service);
                                   Console.WriteLine(composition.Resolve(typeof(IService)) is Service);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableGenericRootBind()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                               T Value { get; }
                           }

                           class Box<T>: IBox<T>
                           {
                               public Box(T value) => Value = value;

                               public T Value { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .RootBind<IBox<string?>>().To<Box<string?>>();
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Resolve<IBox<string?>>().Value is null);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableFactoryRootBind()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                               string? Name { get; }
                           }

                           class Service: IService
                           {
                               public Service(string? name) => Name = name;

                               public string? Name { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .RootBind<IService?>().To(ctx =>
                                       {
                                           ctx.Inject(out string? name);
                                           return new Service(name);
                                       });
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Resolve<IService?>()!.Name is null);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableRootsForImplementations()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                               string? Name { get; }
                           }

                           class Service: IService
                           {
                               [Ordinal]
                               public void Initialize(string? name) => Name = name;

                               public string? Name { get; private set; } = "initial";
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Roots<IService?>();
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Resolve<Service>().Name is null);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableBuildersForImplementations()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                               string? Name { get; }
                           }

                           class Service: IService
                           {
                               [Ordinal]
                               public void Initialize(string? name) => Name = name;

                               public string? Name { get; private set; } = "initial";
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Builders<IService?>();
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var service = new Service();
                                   new Composition().BuildUp(service);
                                   Console.WriteLine(service.Name is null);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableExportAttribute()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Bindings
                           {
                               [Export]
                               public string Required => "required";

                               [Export]
                               public string? Optional => null;
                           }

                           class Service
                           {
                               public Service(string required, string? optional) =>
                                   IsReady = required == "required" && optional is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind().To<Bindings>()
                                       .Bind().To<Service>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableInjectAttributeOnMembers()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           #pragma warning disable CS8618
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
                           class InjectAttribute: Attribute
                           {
                               public InjectAttribute(object? tag = null, int ordinal = 0)
                               {
                               }
                           }

                           class Service
                           {
                               [Inject("optional")]
                               public string? Optional { get; set; }

                               [Inject("required")]
                               public string Required;

                               public bool IsReady => Optional is null && Required == "required";
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .TagAttribute<InjectAttribute>()
                                       .OrdinalAttribute<InjectAttribute>(1)
                                       .Bind<string>("required").To(_ => "required")
                                       .Bind<string?>("optional").To(_ => (string?)null)
                                       .Bind().To<Service>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableInjectAttributeOnMethodParameter()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
                           class InjectAttribute: Attribute
                           {
                               public InjectAttribute(object? tag = null, int ordinal = 0)
                               {
                               }
                           }

                           class Service
                           {
                               public string? Optional { get; private set; } = "initial";

                               [Ordinal]
                               public void Initialize([Inject("optional")] string? optional) => Optional = optional;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .TagAttribute<InjectAttribute>()
                                       .OrdinalAttribute<InjectAttribute>(1)
                                       .Bind<string?>("optional").To(_ => (string?)null)
                                       .Bind().To<Service>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.Optional is null);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableSetAndDictionaryBclTypes()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IService
                           {
                           }

                           class ServiceDependency: IService
                           {
                           }

                           class Service
                           {
                               public Service(
                                   ISet<string?> set,
                                   HashSet<string?> hashSet,
                                   Dictionary<string, IService?> dictionary,
                                   IReadOnlyDictionary<string, IService?> readOnlyDictionary) =>
                                   IsReady =
                                       set.Single() == "dependency"
                                       && hashSet.Single() == "dependency"
                                       && dictionary["item"] is ServiceDependency
                                       && readOnlyDictionary["item"] is ServiceDependency;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "dependency")
                                       .Bind<IService>().To<ServiceDependency>()
                                       .Bind(Tag.Unique).To((IService service) => new KeyValuePair<string, IService?>("item", service))
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableImmutableCollections()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Collections.Immutable;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(
                                   IImmutableList<string?> list,
                                   ImmutableArray<string?> array) =>
                                   IsReady = list.Single() == "dependency" && array.Single() == "dependency";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "dependency")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableMemoryAndReadOnlyMemory()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(Memory<string?> memory, ReadOnlyMemory<string?> readOnlyMemory) =>
                                   IsReady = memory.ToArray().Single() is null && readOnlyMemory.ToArray().Single() is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTaggedNullableSimplifiedFactoryParameter()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? optional) => IsReady = optional is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService([Tag("optional")] string required) => IsReady = required == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>("optional").To(_ => "required")
                                       .Bind<string?>("optional").To(_ => (string?)null)
                                       .Bind<Service>().To(([Tag("optional")] string? optional) => new Service(optional))
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportExplicitNullableSimplifiedFactoryParameter()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? optional) => IsReady = optional is null;

                               public bool IsReady { get; }
                           }

                           class RequiredService
                           {
                               public RequiredService(string required) => IsReady = required == "required";

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<string>().To(_ => "required")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Bind().To<string?, Service>(optional => new Service(optional))
                                       .Root<Service>("Service")
                                       .Root<RequiredService>("RequiredService");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service.IsReady);
                                   Console.WriteLine(composition.RequiredService.IsReady);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportTaggedNullableAndNonNullableArgsWithSameRuntimeType()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(
                                   [Tag("required")] string required,
                                   [Tag("optional")] string? optional) =>
                                   IsReady = required == "required" && optional is null;

                               public bool IsReady { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Arg<string>("required", "required")
                                       .Arg<string?>("optional", "optional")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition("required", null).Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldShowNestedNullableTypesInGeneratedCommentsAndMermaidDiagram()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency
                           {
                           }

                           class Dependency: IDependency
                           {
                           }

                           interface IBox<T>
                           {
                           }

                           class Box<T>: IBox<T>
                           {
                               public Box(IReadOnlyList<IDependency?> dependencies)
                               {
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   // ToString = On
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<IBox<TT>>().To<Box<TT>>()
                                       .Root<IBox<IDependency?>>("Box");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var diagram = new Composition().ToString();
                                   Console.WriteLine(diagram.Contains("IBoxᐸIDependency\u0241ᐳ"));
                                   Console.WriteLine(diagram.Contains("IReadOnlyListᐸIDependency\u0241ᐳ"));
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
        result.GeneratedCode.Contains("Sample.IBox<global::Sample.IDependency?>", StringComparison.Ordinal).ShouldBeTrue(result);
        result.GeneratedCode.Contains("Resolve&lt;Sample.IBox&lt;Sample.IDependency?&gt;&gt;()", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldNotGenerateDoubleNullableForNullableGenericDependencySingleton()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Microsoft.Extensions.Logging;
                           using Pure.DI;

                           namespace Microsoft.Extensions.Logging
                           {
                               public interface ILogger<T>
                               {
                               }

                               public interface ILoggerFactory
                               {
                                   ILogger<T> CreateLogger<T>();
                               }

                               public interface ILoggingBuilder
                               {
                               }

                               public sealed class LoggerFactory: ILoggerFactory
                               {
                                   public static ILoggerFactory Create(Action<ILoggingBuilder> configure) => new LoggerFactory();

                                   public ILogger<T> CreateLogger<T>() => new Logger<T>();
                               }

                               public sealed class Logger<T>: ILogger<T>
                               {
                               }
                           }

                           namespace PureDiNullable
                           {
                               public class Class
                               {
                                   public Class(ILogger<Class>? logger) => Logger = logger;

                                   public ILogger<Class>? Logger { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Root<Class>(nameof(Class))
                                           .Bind<ILoggerFactory>().As(Lifetime.Singleton).To(_ => LoggerFactory.Create(builder => { }))
                                           .Bind<ILogger<TT>>().As(Lifetime.Singleton).To(ctx =>
                                           {
                                               ctx.Inject<ILoggerFactory>(out var factory);
                                               return factory.CreateLogger<TT>();
                                           });
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Class.Logger is not null);
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("?? _singleton", StringComparison.Ordinal).ShouldBeFalse(result);
        result.GeneratedCode.Contains("Microsoft.Extensions.Logging.ILogger<global::PureDiNullable.Class>? _singleton", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldNotGenerateDoubleNullableForNullableGenericDependencyScoped()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                           }

                           class Box<T>: IBox<T>
                           {
                           }

                           class Service
                           {
                               public Service(IBox<string>? box) => Box = box;

                               public IBox<string>? Box { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IBox<TT>>().As(Lifetime.Scoped).To<Box<TT>>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.Box is not null);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("?? _scoped", StringComparison.Ordinal).ShouldBeFalse(result);
        result.GeneratedCode.Contains("Sample.Box<string>? _scoped", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldNotGenerateDoubleNullableForNullableArrayDependencySingleton()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IDependency
                           {
                           }

                           class Dependency: IDependency
                           {
                           }

                           class Service
                           {
                               public Service(IDependency[]? dependencies) => Dependencies = dependencies;

                               public IDependency[]? Dependencies { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<IDependency[]?>().As(Lifetime.Singleton).To(ctx =>
                                       {
                                           ctx.Inject(out IDependency[] dependencies);
                                           return dependencies;
                                       })
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.Dependencies is { Length: 1 });
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("[]?? _singleton", StringComparison.Ordinal).ShouldBeFalse(result);
        result.GeneratedCode.Contains("IDependency[]? _singleton", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldGenerateNullableBackingFieldForNonNullableGenericDependencySingleton()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IBox<T>
                           {
                           }

                           class Box<T>: IBox<T>
                           {
                           }

                           class Service
                           {
                               public Service(IBox<string> box) => Box = box;

                               public IBox<string> Box { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IBox<TT>>().As(Lifetime.Singleton).To<Box<TT>>()
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.Box is not null);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("Sample.Box<string>? _singleton", StringComparison.Ordinal).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportNullableOnCannotResolvePartialMethod()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value is null;

                               public bool IsReady { get; }
                           }

                           internal partial class Composition
                           {
                               private partial T OnCannotResolve<T>(object? tag, Lifetime lifetime)
                               {
                                   if (typeof(T) == typeof(string))
                                   {
                                       return default!;
                                   }

                                   throw new Exception("Cannot resolve.");
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   // OnCannotResolve = On
                                   DI.Setup("Composition")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableOnDependencyInjectionPartialMethod()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value == "hook";

                               public bool IsReady { get; }
                           }

                           internal partial class Composition
                           {
                               private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
                               {
                                   if (typeof(T) == typeof(string) && value is null)
                                   {
                                       return (T)(object)"hook";
                                   }

                                   return value;
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   // OnDependencyInjection = On
                                   DI.Setup("Composition")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableOnNewInstancePartialMethod()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value == "created";

                               public bool IsReady { get; }
                           }

                           internal partial class Composition
                           {
                               partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime)
                               {
                                   if (typeof(T) == typeof(string) && value is null)
                                   {
                                       value = (T)(object)"created";
                                   }
                               }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Hint(Hint.OnNewInstance, "On")
                                       .Bind<string?>().To(_ => (string?)null)
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.IsReady);
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldGenerateNullableTagInPartialApiMemberDeclarations()
    {
        // Given

        // When
        var result = await """
                           #nullable enable annotations
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class Service
                           {
                               public Service(string? value) => IsReady = value is null;

                               public bool IsReady { get; }
                           }

                           internal partial class Composition
                           {
                               partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime)
                               {
                               }

                               private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime) => value;

                               private partial T OnCannotResolve<T>(object? tag, Lifetime lifetime) => default!;
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   // OnDependencyInjection = On
                                   // OnCannotResolve = On
                                   DI.Setup("Composition")
                                       .Hint(Hint.OnNewInstance, "On")
                                       .Root<Service>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains("partial void OnNewInstance<T>(ref T value, object? tag, global::Pure.DI.Lifetime lifetime);", StringComparison.Ordinal).ShouldBeTrue(result);
        result.GeneratedCode.Contains("private partial T OnDependencyInjection<T>(in T value, object? tag, global::Pure.DI.Lifetime lifetime);", StringComparison.Ordinal).ShouldBeTrue(result);
        result.GeneratedCode.Contains("private partial T OnCannotResolve<T>(object? tag, global::Pure.DI.Lifetime lifetime);", StringComparison.Ordinal).ShouldBeTrue(result);
    }

}
