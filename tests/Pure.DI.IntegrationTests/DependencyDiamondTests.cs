using System.Text;

namespace Pure.DI.IntegrationTests;

/// <summary>
/// A graph where each instance depends on the two previous ones has a number of dependency paths
/// that grows exponentially with its depth, see #171. Code that creates a shared instance in place
/// at each of its use sites repeats it once per path, so such instances are created by local functions.
/// </summary>
public class DependencyDiamondTests
{
    private const int Count = 21;

    [Theory]
    [InlineData("PerResolve", "", "S20", "composition.Root.S0.Label")]
    [InlineData("Singleton", ".Hint(Hint.ThreadSafe, \"Off\")", "S20", "composition.Root.S0.Label")]
    [InlineData("Scoped", ".Hint(Hint.ThreadSafe, \"Off\")", "S20", "composition.Root.S0.Label")]
    [InlineData("PerResolve", "", "Owned<S20>", "composition.Root.Value.S0.Label")]
    [InlineData("PerResolve", "", "Func<Owned<S20>>", "composition.Root().Value.S0.Label")]
    [InlineData("Singleton", "", "Func<Owned<S20>>", "composition.Root().Value.S0.Label")]
    public async Task ShouldCreateInstancesOfDiamondInFewPlaces(string lifetime, string hint, string rootType, string value)
    {
        // Given

        // When
        var result = await CreateSetup(
                Count,
                lifetime,
                hint,
                rootType,
                $"Console.WriteLine({value});",
                "public S0() => Label = \"ok\";")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["ok"], result);
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("new global::Sample.S0(")).ShouldBeLessThan(Count, result);
    }

    [Fact]
    public async Task ShouldCreateArgumentConsumingDiamondUnderOwnedInFewPlaces()
    {
        // Given

        // When
        var result = await CreateSetup(
                Count,
                "PerResolve",
                "",
                "Func<string, Owned<S20>>",
                """Console.WriteLine(composition.Root("x").Value.S0.Label + " " + composition.Root("y").Value.S0.Label);""",
                "public S0(string label) => Label = label;")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        // Each call of the Func creates its own PerResolve instances under its own Owned.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["x y"], result);
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("new global::Sample.S0(")).ShouldBeLessThan(Count, result);
    }

    [Fact]
    public async Task ShouldAddDisposableOfDiamondToOwnedOnce()
    {
        // Given

        // When
        var result = await CreateSetup(
                8,
                "PerResolve",
                "",
                "Owned<S7>",
                """
                var owned = composition.Root;
                Console.WriteLine(owned.Value.S0.Label);
                owned.Dispose();
                Console.WriteLine(S0.Disposed);
                """,
                "public S0() => Label = \"ok\"; public static int Disposed; public void Dispose() => Disposed++;",
                ": IDisposable")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        // A local function would be shared with the code that has no Owned to collect the instance.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["ok", "1"], result);
    }

    private static string CreateSetup(int count, string lifetime, string hint, string rootType, string main, string s0, string s0Base = "")
    {
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        classes.AppendLine($"sealed class S0 {s0Base} {{ {s0} public string Label {{ get; }} }}");
        classes.AppendLine("sealed class S1 { public S1(S0 s0) => S0 = s0; public S0 S0 { get; } }");
        for (var i = 2; i < count; i++)
        {
            classes.AppendLine($"sealed class S{i} {{ public S{i}(S{i - 1} a, S{i - 2} b) => S0 = a.S0; public S0 S0 {{ get; }} }}");
        }

        for (var i = 0; i < count; i++)
        {
            bindings.AppendLine($".Bind<S{i}>().As({lifetime}).To<S{i}>()");
        }

        return """
               using System;
               using Pure.DI;
               using static Pure.DI.Lifetime;

               namespace Sample
               {
                   public static class Program
                   {
                       public static void Main()
                       {
                           var composition = new Composition();
                           #main#
                       }
                   }

                   #classes#

                   partial class Composition
                   {
                       private void Setup() =>
                           DI.Setup(nameof(Composition))
                               #hint#
                               #bindings#
                               .Root<#rootType#>("Root");
                   }
               }
               """
            .Replace("#main#", main)
            .Replace("#classes#", classes.ToString())
            .Replace("#hint#", hint)
            .Replace("#bindings#", bindings.ToString())
            .Replace("#rootType#", rootType);
    }
}
