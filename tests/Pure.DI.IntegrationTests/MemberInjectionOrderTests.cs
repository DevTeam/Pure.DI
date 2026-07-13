namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests the execution order of field, property, and method injections.
/// </summary>
public class MemberInjectionOrderTests
{
    [Fact]
    public async Task ShouldOrderFieldPropertyAndMethodByOrdinal()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency { }

                               class Service
                               {
                                   [Ordinal(0)] public Dependency Field = null!;

                                   private Dependency? _property;

                                   [Ordinal(1)]
                                   public Dependency Property
                                   {
                                       set
                                       {
                                           Console.WriteLine(Field is null ? "property-before-field" : "property-after-field");
                                           _property = value;
                                       }
                                   }

                                   [Ordinal(2)]
                                   public void Initialize(Dependency dependency) =>
                                       Console.WriteLine(_property is null ? "method-before-property" : "method-after-property");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Root<Service>("Root");
                                   }
                               }

                               public static class Program
                               {
                                   public static void Main() => _ = new Composition().Root;
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["property-after-field", "method-after-property"], result);
    }

    [Fact]
    public async Task ShouldUseMethodParameterOrdinal()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency { }

                               class Service
                               {
                                   public void Last([Ordinal(2)] Dependency dependency) => Console.WriteLine("last");

                                   public void First([Ordinal(0)] Dependency dependency) => Console.WriteLine("first");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Root<Service>("Root");
                                   }
                               }

                               public static class Program
                               {
                                   public static void Main() => _ = new Composition().Root;
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["first", "last"], result);
    }

    [Fact]
    public async Task ShouldUseLowestMethodParameterOrdinal()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class FirstDependency { }
                               class SecondDependency { }
                               class ThirdDependency { }

                               class Service
                               {
                                   public void Mixed(
                                       [Ordinal(5)] FirstDependency first,
                                       [Ordinal(-1)] SecondDependency second) => Console.WriteLine("mixed");

                                   [Ordinal(0)]
                                   public void Middle(ThirdDependency dependency) => Console.WriteLine("middle");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Root<Service>("Root");
                                   }
                               }

                               public static class Program
                               {
                                   public static void Main() => _ = new Composition().Root;
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["mixed", "middle"], result);
    }

    [Fact]
    public async Task ShouldPreferMethodOrdinalOverParameterOrdinals()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency { }

                               class Service
                               {
                                   [Ordinal(1)]
                                   public void Last([Ordinal(-1)] Dependency dependency) => Console.WriteLine("last");

                                   [Ordinal(0)]
                                   public void First(Dependency dependency) => Console.WriteLine("first");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Root<Service>("Root");
                                   }
                               }

                               public static class Program
                               {
                                   public static void Main() => _ = new Composition().Root;
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["first", "last"], result);
    }

    [Fact]
    public async Task ShouldAssignRequiredFieldsBeforeRequiredInitProperties()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency { }

                               class Service
                               {
                                   [Ordinal(100)] public required Dependency Field;

                                   private Dependency? _property;

                                   [Ordinal(-100)]
                                   public required Dependency Property
                                   {
                                       get => _property!;
                                       init
                                       {
                                           Console.WriteLine(Field is null ? "property-before-field" : "field-before-property");
                                           _property = value;
                                       }
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Root<Service>("Root");
                                   }
                               }

                               public static class Program
                               {
                                   public static void Main() => _ = new Composition().Root;
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["field-before-property"], result);
    }

    [Fact]
    public async Task ShouldRunRegularMembersAfterObjectInitializer()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency { }

                               class Service
                               {
                                   public required Dependency Field;

                                   public required Dependency Property { get; init; }

                                   [Ordinal(-100)]
                                   public void Initialize(Dependency dependency) =>
                                       Console.WriteLine(Field is not null && Property is not null ? "ready" : "not-ready");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Root<Service>("Root");
                                   }
                               }

                               public static class Program
                               {
                                   public static void Main() => _ = new Composition().Root;
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["ready"], result);
    }

    [Fact]
    public async Task ShouldOrderInheritedMembersByOrdinal()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency { }

                               class BaseService
                               {
                                   [Ordinal(1)]
                                   public void InitializeBase(Dependency dependency) => Console.WriteLine("base");
                               }

                               class Service : BaseService
                               {
                                   [Ordinal(0)]
                                   public void InitializeDerived(Dependency dependency) => Console.WriteLine("derived");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Root<Service>("Root");
                                   }
                               }

                               public static class Program
                               {
                                   public static void Main() => _ = new Composition().Root;
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["derived", "base"], result);
    }

    [Fact]
    public async Task ShouldSupportNegativeMemberOrdinals()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency { }

                               class Service
                               {
                                   [Ordinal(0)] public void Last(Dependency dependency) => Console.WriteLine("zero");
                                   [Ordinal(-2)] public void First(Dependency dependency) => Console.WriteLine("minus-two");
                                   [Ordinal(-1)] public void Second(Dependency dependency) => Console.WriteLine("minus-one");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Root<Service>("Root");
                                   }
                               }

                               public static class Program
                               {
                                   public static void Main() => _ = new Composition().Root;
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["minus-two", "minus-one", "zero"], result);
    }

    [Fact]
    public async Task ShouldUseKindAndDeclarationOrderForEqualMemberOrdinals()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency { }

                               class Service
                               {
                                   [Ordinal(0)] public Dependency Field = null!;

                                   [Ordinal(0)]
                                   public Dependency Property
                                   {
                                       set => Console.WriteLine(Field is null ? "property-before-field" : "property");
                                   }

                                   [Ordinal(0)] public void First(Dependency dependency) => Console.WriteLine("first");

                                   [Ordinal(0)] public void Second(Dependency dependency) => Console.WriteLine("second");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Root<Service>("Root");
                                   }
                               }

                               public static class Program
                               {
                                   public static void Main() => _ = new Composition().Root;
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["property", "first", "second"], result);
    }
}
