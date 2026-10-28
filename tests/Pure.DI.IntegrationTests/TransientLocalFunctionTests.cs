using System.Text;

namespace Pure.DI.IntegrationTests;

/// <summary>
/// A graph where each transient instance depends on two instances of the previous level creates
/// a number of instances that doubles with each level. Code that creates each of them in place doubles too,
/// so the code of a large transient instance used in several places is moved to a Create...() local function, see #167.
/// </summary>
public class TransientLocalFunctionTests
{
    private const int Depth = 10;
    private const int LeafCount = 1 << Depth;

    [Theory]
    [InlineData("Auto", true)]
    [InlineData("On", true)]
    [InlineData("Off", false)]
    public async Task ShouldCreateTransientDiamondInFewPlaces(string mode, bool hasLocalFunctions)
    {
        // Given

        // When
        var result = await CreateDiamond(
                mode,
                "sealed class L0 { public static int Count; public L0() => Count++; }",
                $"L{Depth}",
                "_ = composition.Root; Console.WriteLine(L0.Count);")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([LeafCount.ToString()], result);
        if (hasLocalFunctions)
        {
            CountOf(result, "new global::Sample.L0(").ShouldBeLessThan(LeafCount / 8, result);
            result.GeneratedCode.ShouldContain($"global::Sample.L{Depth - 1} CreateL{Depth - 1}", Case.Sensitive, result);
        }
        else
        {
            CountOf(result, "new global::Sample.L0(").ShouldBe(LeafCount, result);
            result.GeneratedCode.ShouldNotContain(" CreateL", Case.Sensitive, result);
        }
    }

    [Fact]
    public async Task ShouldKeepSmallTransientDiamondInPlace()
    {
        // Given

        // When
        var result = await CreateDiamond(
                "Auto",
                "sealed class L0 { public static int Count; public L0() => Count++; }",
                "L3",
                "_ = composition.Root; Console.WriteLine(L0.Count);",
                depth: 3)
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        // Repeating a small piece of code is cheaper than a call.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["8"], result);
        result.GeneratedCode.ShouldNotContain(" CreateL", Case.Sensitive, result);
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("On")]
    public async Task ShouldShareOnePerBlockInstanceAcrossTransientDiamond(string mode)
    {
        // Given

        // When
        var result = await CreateDiamond(
                mode,
                """
                sealed class Shared { public static int Count; public Shared() => Count++; }

                sealed class L0
                {
                    public static Shared? First;
                    public static bool AllSame = true;

                    public L0(Shared shared)
                    {
                        First ??= shared;
                        AllSame &= ReferenceEquals(First, shared);
                    }
                }
                """,
                $"L{Depth}",
                "_ = composition.Root; Console.WriteLine(Shared.Count + \" \" + L0.AllSame);",
                ".Bind<Shared>().As(Lifetime.PerBlock).To<Shared>()")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        // A local function would create the PerBlock instance anew on each call.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1 True"], result);
        result.GeneratedCode.ShouldNotContain(" CreateL", Case.Sensitive, result);
    }

#if ROSLYN5_6_OR_GREATER
    [Fact]
    public async Task ShouldCreateRefStructDiamondInPlace()
    {
        // Given
        var classes = new StringBuilder();
        classes.AppendLine("ref struct R0 { public static int Count; public R0(int _) => Count++; }");
        for (var i = 1; i <= 6; i++)
        {
            classes.AppendLine($"ref struct R{i} {{ public R{i}(R{i - 1} first, R{i - 1} second) {{ }} }}");
        }

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               /*classes*/

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.Resolve, "Off")
                                           .Hint(Hint.LocalFunctions, "On")
                                           .Bind<R0>().To(_ => new R0(0))
                                           .Root<R6>("Root");
                               }

                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       _ = composition.Root;
                                       Console.WriteLine(R0.Count);
                                   }
                               }
                           }
                           """.Replace("/*classes*/", classes.ToString()).RunAsync(new Options(
                               LanguageVersion.CSharp14,
                               PreprocessorSymbols: ["NET", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER"]));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["64"], result);
        result.GeneratedCode.ShouldNotContain(" CreateR", Case.Sensitive, result);
    }
#endif

    [Fact]
    public async Task ShouldCreateFactoryOfTransientDiamondInPlace()
    {
        // Given

        // When
        var result = await CreateDiamond(
                "On",
                "sealed class L0 { public static int Count; public L0() => Count++; }",
                $"L{Depth}",
                "_ = composition.Root; Console.WriteLine(L0.Count);",
                """
                .Bind<L5>().To(ctx =>
                {
                    ctx.Inject(out L4 first);
                    ctx.Inject(out L4 second);
                    return new L5(first, second);
                })
                """)
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        // A factory is created in place, while its dependencies are created by local functions.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([LeafCount.ToString()], result);
        result.GeneratedCode.ShouldNotContain(" CreateL5", Case.Sensitive, result);
        result.GeneratedCode.ShouldContain(" CreateL4", Case.Sensitive, result);
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("On")]
    public async Task ShouldPassInjectionTagsThroughTransientLocalFunctions(string mode)
    {
        // Given

        // When
        var result = await CreateDiamond(
                mode,
                """
                sealed class Name
                {
                    public Name(string value) => Value = value;

                    public string Value { get; }
                }

                sealed class L0
                {
                    public static int A;
                    public static int B;

                    public L0([Tag("a")] Name first, [Tag("b")] Name second)
                    {
                        if (first.Value == "a") A++;
                        if (second.Value == "b") B++;
                    }
                }
                """,
                $"L{Depth}",
                "_ = composition.Root; _ = composition.Other; Console.WriteLine(L0.A + \" \" + L0.B);",
                """
                .Bind<Name>(Tag.Any).To(ctx => new Name(ctx.Tag?.ToString() ?? ""))
                .Bind<L9>(Tag.Any).To<L9>()
                .Root<L9>("Other", "other")
                """)
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        var count = LeafCount + LeafCount / 2;
        result.StdOut.ShouldBe([$"{count} {count}"], result);
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("On")]
    public async Task ShouldAddEachTransientOfDiamondToAccumulator(string mode)
    {
        // Given

        // When
        var result = await CreateDiamond(
                mode,
                """
                interface IAcc { }

                sealed class MyAcc : List<IAcc> { }

                sealed class L0 : IAcc { }
                """,
                $"(MyAcc acc, L{Depth} root)",
                "var (acc, _) = composition.Root; Console.WriteLine(acc.Count);",
                ".Accumulate<IAcc, MyAcc>(Lifetime.Transient)")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([LeafCount.ToString()], result);
        CountOf(result, "new global::Sample.L0(").ShouldBeLessThan(LeafCount / 8, result);
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("On")]
    public async Task ShouldAddEachTransientOfDiamondToOwned(string mode)
    {
        // Given

        // When
        var result = await CreateDiamond(
                mode,
                "sealed class L0 : IDisposable { public static int Disposed; public void Dispose() => Disposed++; }",
                $"Owned<L{Depth}>",
                "var owned = composition.Root; owned.Dispose(); Console.WriteLine(L0.Disposed);")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([LeafCount.ToString()], result);
        CountOf(result, "new global::Sample.L0(").ShouldBeLessThan(LeafCount / 8, result);
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("On")]
    public async Task ShouldInterceptCreationOfEachTransientOfDiamond(string mode)
    {
        // Given

        // When
        var result = await CreateDiamond(
                mode,
                """
                sealed class L0 { }

                partial class Composition
                {
                    public static int Count;

                    partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime)
                    {
                        if (value is L0) Count++;
                    }
                }
                """,
                $"L{Depth}",
                "_ = composition.Root; Console.WriteLine(Composition.Count);",
                ".Hint(Hint.OnNewInstance, \"On\")")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([LeafCount.ToString()], result);
        CountOf(result, "new global::Sample.L0(").ShouldBeLessThan(LeafCount / 8, result);
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("On")]
    public async Task ShouldInterceptInjectionOfEachTransientOfDiamond(string mode)
    {
        // Given

        // When
        var result = await CreateDiamond(
                mode,
                """
                sealed class L0 { }

                partial class Composition
                {
                    public static int Count;

                    private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
                    {
                        if (value is L0) Count++;
                        return value;
                    }
                }
                """,
                $"L{Depth}",
                "_ = composition.Root; Console.WriteLine(Composition.Count);",
                ".Hint(Hint.OnDependencyInjection, \"On\")")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([LeafCount.ToString()], result);
        CountOf(result, "new global::Sample.L0(").ShouldBeLessThan(LeafCount / 8, result);
    }

    [Fact]
    public async Task ShouldSupportLocalFunctionsHintInComment()
    {
        // Given

        // When
        var result = await CreateDiamond(
                "",
                "sealed class L0 { }",
                $"L{Depth}",
                "_ = composition.Root;",
                comment: "// LocalFunctions = Off")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        CountOf(result, "new global::Sample.L0(").ShouldBe(LeafCount, result);
    }

    private static int CountOf(Result result, string text) =>
        result.GeneratedCode.Split(Environment.NewLine).Sum(line => CountOf(line, text));

    private static int CountOf(string line, string text)
    {
        var count = 0;
        for (var index = line.IndexOf(text, StringComparison.Ordinal); index >= 0; index = line.IndexOf(text, index + text.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static string CreateDiamond(
        string mode,
        string leaf,
        string rootType,
        string main,
        string bindings = "",
        int depth = Depth,
        string comment = "")
    {
        var classes = new StringBuilder();
        classes.AppendLine(leaf);
        for (var i = 1; i <= depth; i++)
        {
            classes.AppendLine($"sealed class L{i} {{ public L{i}(L{i - 1} first, L{i - 1} second) {{ }} }}");
        }

        return """
               using System;
               using System.Collections.Generic;
               using Pure.DI;

               namespace Sample
               {
                   #classes#

                   partial class Composition
                   {
                       private void Setup() =>
                           #comment#
                           DI.Setup(nameof(Composition))
                               #mode#
                               #bindings#
                               .Root<#rootType#>("Root");
                   }

                   public static class Program
                   {
                       public static void Main()
                       {
                           var composition = new Composition();
                           #main#
                       }
                   }
               }
               """
            .Replace("#classes#", classes.ToString())
            .Replace("#comment#", comment)
            .Replace("#mode#", mode == "" ? "" : $".Hint(Hint.LocalFunctions, \"{mode}\")")
            .Replace("#bindings#", bindings)
            .Replace("#rootType#", rootType)
            .Replace("#main#", main);
    }
}
