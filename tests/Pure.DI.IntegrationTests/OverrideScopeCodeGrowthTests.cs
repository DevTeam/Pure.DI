namespace Pure.DI.IntegrationTests;

using System.Text;
using Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;

/// <summary>
/// Probes the code growth of acyclic graphs whose nodes are reached both under a delegate that overrides a value
/// and outside it. Each level reaches the next one through Func&lt;T1, T&gt; and directly, so a node is reached
/// with each subset of the values that the delegates above it override. When the node reads these values,
/// each subset is an override scope of its own, and the number of the scopes doubles per level.
/// The other growth tests reach a node only through delegates, so all the paths to it override the same values.
/// Each case also walks the graph and checks the values each node gets.
/// </summary>
[Trait("Category", "CodeSize")]
public class OverrideScopeCodeGrowthTests(ITestOutputHelper output)
{
    // A local function is created when the code repeated in place is large enough, which happens once per several levels,
    // so the depths are multiples of the number of these levels.
    private static readonly int[] Depths = [8, 16, 24];
    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromSeconds(30);
    // Equal steps in the number of levels must add approximately equal amounts of code.
    private const double MaxGrowthRatio = 1.35;
    // The code that grows with the square of the number of levels adds 5/3 times as much in the last of two equal steps.
    private const double MaxQuadraticGrowthRatio = MaxGrowthRatio * 5 / 3;

    public static IEnumerable<object[]> Cases() =>
        from arguments in new[] { Arguments.OnePerLevelReadByLeaf, Arguments.OnePerLevelReadByNextLevel, Arguments.SameReadByLeaf, Arguments.SameReadByEachLevel, Arguments.SameReadByEachLevelThroughPerBlock }
        from lifetime in new[] { "Transient", "PerBlock" }
        from mode in new[] { "Auto", "On" }
        select new object[] { arguments, lifetime, mode };

    [Theory]
    [MemberData(nameof(Cases))]
    public Task ShouldGrowLinearlyForNodesReachedUnderDelegateAndOutsideIt(Arguments arguments, string lifetime, string mode) =>
        CheckGrowth(arguments, lifetime, mode);

    private async Task CheckGrowth(Arguments arguments, string lifetime, string mode)
    {
        // All the depths are measured before the checks, so a failure shows the growth too.
        var measurements = new List<Measurement>();
        var problems = new List<string>();
        foreach (var depth in Depths)
        {
            Result result;
            try
            {
                result = await Create(depth, arguments, lifetime, mode).RunAsync(new Options(Options.ModernLanguageVersion), GenerationTimeout);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"{depth} levels are not generated in {GenerationTimeout}; previous: {string.Join("; ", measurements)}");
            }

            result.Errors.ShouldBeEmpty(result);
            foreach (var warning in result.Warnings.Where(i => i.Id != LogId.WarningRootCodeIsTooLarge).Select(i => i.Id).Distinct())
            {
                problems.Add($"{depth} levels are generated with the warning {warning}");
            }

            measurements.Add(Measure(depth, result));
            var expected = string.Join(" ", GetMasks(depth).Select(mask => GetExpectedWalk(depth, arguments, mask)));
            if (result.StdOut.Length != 1 || result.StdOut[0] != expected)
            {
                problems.Add($"{depth} levels walk{Environment.NewLine}  as {string.Join(Environment.NewLine, result.StdOut)}{Environment.NewLine}  instead of {expected}");
            }
        }

        // The last node that reads a value of each level gets them through each level, so the code grows with the square
        // of the number of levels, while the number of the nodes it creates grows linearly.
        var tokensRatio = GrowthRatio(measurements.Select(i => i.Tokens).ToArray());
        if (tokensRatio >= (arguments == Arguments.OnePerLevelReadByLeaf ? MaxQuadraticGrowthRatio : MaxGrowthRatio))
        {
            problems.Add($"Tokens grow by the ratio {tokensRatio:F2}");
        }

        var constructionsRatio = GrowthRatio(measurements.Select(i => i.Constructions).ToArray());
        if (constructionsRatio >= MaxGrowthRatio)
        {
            problems.Add($"Constructions of nodes grow by the ratio {constructionsRatio:F2}");
        }

        if (measurements.Any(i => i.SizeWarnings > 0))
        {
            problems.Add("The code of a root is too large");
        }

        problems.ShouldBeEmpty(string.Join(Environment.NewLine, measurements));
    }

    private Measurement Measure(int depth, Result result)
    {
        var namespaces = CSharpSyntaxTree.ParseText(result.GeneratedCode).GetRoot().DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>().Where(i => i.Name.ToString() == "Sample").ToArray();
        namespaces.ShouldNotBeEmpty(result);
        var constructions = namespaces.SelectMany(i => i.DescendantNodes()).OfType<ObjectCreationExpressionSyntax>()
            .Count(i => i.Type.ToString().Contains(".N"));
        var measurement = new Measurement(
            depth,
            namespaces.Sum(i => i.DescendantTokens().Count()),
            constructions,
            result.Warnings.Count(i => i.Id == LogId.WarningRootCodeIsTooLarge));
        output.WriteLine(measurement.ToString());
        return measurement;
    }

    private static double GrowthRatio(IReadOnlyList<int> sizes)
    {
        var firstStep = sizes[1] - sizes[0];
        var lastStep = sizes[2] - sizes[1];
        return firstStep <= 0 ? lastStep <= 0 ? 1 : double.PositiveInfinity : (double)lastStep / firstStep;
    }

    // The bit of a level is set when the walk goes down from this level through the delegate.
    private static int[] GetMasks(int depth)
    {
        var all = 0;
        var odd = 0;
        for (var level = 1; level <= depth; level++)
        {
            all |= 1 << level;
            if (level % 2 == 1)
            {
                odd |= 1 << level;
            }
        }

        return [0, all, odd, all & ~odd];
    }

    private static bool IsSame(Arguments arguments) => arguments is Arguments.SameReadByLeaf or Arguments.SameReadByEachLevel or Arguments.SameReadByEachLevelThroughPerBlock;

    private static bool IsReadByLeaf(Arguments arguments) => arguments is Arguments.OnePerLevelReadByLeaf or Arguments.SameReadByLeaf;

    private static string Argument(Arguments arguments, int level) => IsSame(arguments) ? "A" : $"A{level}";

    private static string Create(int depth, Arguments arguments, string lifetime, string mode)
    {
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        foreach (var argument in Enumerable.Range(1, depth).Select(level => Argument(arguments, level)).Distinct())
        {
            classes.AppendLine($"    sealed class {argument} {{ public {argument}(string value) => Value = value; public string Value {{ get; }} }}");
            // The value that a node gets outside the delegates.
            bindings.AppendLine($"            .Bind<{argument}>().To(_ => new {argument}(\"d\"))");
        }

        if (arguments == Arguments.SameReadByEachLevelThroughPerBlock)
        {
            classes.AppendLine("    sealed class Holder { public Holder(A a) => Value = a.Value; public string Value { get; } }");
            bindings.AppendLine("            .Bind<Holder>().As(Lifetime.PerBlock).To<Holder>()");
        }

        for (var level = 0; level <= depth; level++)
        {
            // The values that the node reads.
            var values = arguments switch
            {
                Arguments.OnePerLevelReadByLeaf when level == 0 => Enumerable.Range(1, depth).Reverse().Select(i => $"A{i}").ToArray(),
                Arguments.OnePerLevelReadByNextLevel when level < depth => [$"A{level + 1}"],
                Arguments.SameReadByLeaf when level == 0 => ["A"],
                Arguments.SameReadByEachLevel => ["A"],
                Arguments.SameReadByEachLevelThroughPerBlock => ["Holder"],
                _ => []
            };

            var parameters = values.Select((type, index) => $"{type} value{index}").ToList();
            var assignments = string.Join(" ", values.Select((_, index) => $"_value{index} = value{index};"));
            var fields = string.Join(" ", values.Select((type, index) => $"private readonly {type} _value{index};"));
            var current = string.Concat(values.Select((_, index) => $"{{_value{index}.Value}}"));
            string walk;
            if (level == 0)
            {
                walk = $"$\"{current}\"";
            }
            else
            {
                var argument = Argument(arguments, level);
                parameters.Add($"Func<{argument}, N{level - 1}> left");
                parameters.Add($"N{level - 1} right");
                fields += $" private readonly Func<{argument}, N{level - 1}> _left; private readonly N{level - 1} _right;";
                assignments += " _left = left; _right = right;";
                walk = $"$\"{current}\" + (((mask >> {level}) & 1) == 1 ? _left(new {argument}(\"{level}\")).Walk(mask) : _right.Walk(mask))";
            }

            classes.AppendLine($$"""
                    sealed class N{{level}}
                    {
                        {{fields}}
                        public N{{level}}({{string.Join(", ", parameters)}}) { {{assignments}} }
                        public string Walk(int mask) => {{walk}};
                    }
                """);
            if (lifetime != "Transient")
            {
                bindings.AppendLine($"            .Bind<N{level}>().As(Lifetime.{lifetime}).To<N{level}>()");
            }
        }

        var walks = string.Join(" + \" \" + ", GetMasks(depth).Select(mask => $"root.Walk({mask})"));
        return $$"""
            using System;
            using Pure.DI;
            namespace Sample
            {
            {{classes}}
                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Hint(Hint.LocalFunctions, "{{mode}}")
            {{bindings}}
                        .Root<N{{depth}}>("Root");
                }
                public static class Program
                {
                    public static void Main()
                    {
                        var root = new Composition().Root;
                        Console.WriteLine({{walks}});
                    }
                }
            }
            """;
    }

    // Simulates the walk: a node reads the argument of the nearest delegate above it that overrides the value.
    private static string GetExpectedWalk(int depth, Arguments arguments, int mask)
    {
        var walk = new StringBuilder();
        for (var level = depth; level >= 0; level--)
        {
            switch (arguments)
            {
                case Arguments.OnePerLevelReadByLeaf when level == 0:
                    for (var i = depth; i >= 1; i--)
                    {
                        walk.Append(IsSet(i) ? $"{i}" : "d");
                    }

                    break;

                case Arguments.OnePerLevelReadByNextLevel when level < depth:
                    walk.Append(IsSet(level + 1) ? $"{level + 1}" : "d");
                    break;

                case Arguments.SameReadByLeaf when level == 0:
                case Arguments.SameReadByEachLevel:
                case Arguments.SameReadByEachLevelThroughPerBlock:
                    var nearest = Enumerable.Range(level + 1, depth - level).Where(IsSet).Select(i => $"{i}").FirstOrDefault();
                    walk.Append(nearest ?? "d");
                    break;
            }
        }

        return walk.ToString();

        bool IsSet(int level) => ((mask >> level) & 1) == 1;
    }

    private sealed record Measurement(int Depth, int Tokens, int Constructions, int SizeWarnings)
    {
        public override string ToString() =>
            $"levels={Depth} tokens={Tokens} nodeConstructions={Constructions} DIW016={SizeWarnings}";
    }

    public enum Arguments
    {
        // Each delegate overrides a type of its own, and the last node reads the values of all the types.
        OnePerLevelReadByLeaf,
        // Each delegate overrides a type of its own, which only the node of the next level reads.
        OnePerLevelReadByNextLevel,
        // All the delegates override the same type, which the last node reads.
        SameReadByLeaf,
        // All the delegates override the same type, which each node reads.
        SameReadByEachLevel,
        // All the delegates override the same type, which each node reads through a PerBlock instance that the nodes of a block share.
        SameReadByEachLevelThroughPerBlock
    }
}
