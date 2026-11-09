namespace Pure.DI.IntegrationTests;

using System.Diagnostics;
using System.Text;
using Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;

/// <summary>
/// Probes the code growth of graphs with cycles through lambdas, such as screens that open each other, see #192.
/// The other growth tests use acyclic graphs. Here each screen opens the next screens of a ring,
/// so the number of navigation paths to a screen grows exponentially with the number of screens
/// when the screens open two of them, and quadratically when a hub opens each screen of a ring.
/// Each case also navigates along the cycle and checks the values each screen gets.
/// </summary>
[Trait("Category", "CodeSize")]
public class CycleCodeGrowthTests(ITestOutputHelper output)
{
    private static readonly int[] Sizes = [4, 6, 8];
    private static readonly int[] HubSizes = [4, 8, 12];
    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromMinutes(2);
    private const int Steps = 9;
    // Equal steps in the number of screens must add approximately equal amounts of code.
    private const double MaxGrowthRatio = 1.35;

    public static IEnumerable<object[]> EdgeCases() =>
        from edge in new[] { CycleEdge.FuncWithArguments, CycleEdge.FuncWithArgumentsByLet, CycleEdge.Func, CycleEdge.Lazy }
        from lifetime in new[] { "Transient", "PerBlock" }
        from mode in new[] { "Auto", "On" }
        select new object[] { edge, lifetime, mode };

    public static IEnumerable<object[]> ArgumentCases() =>
        from edge in new[] { CycleEdge.FuncWithArguments, CycleEdge.FuncWithArgumentsByLet }
        from mode in new[] { "Auto", "On" }
        select new object[] { edge, mode };

    // ctx.Let passes a value to the current level only, which is Owned<T> rather than the screen in it, see #191.
    public static IEnumerable<object[]> OwnedCases() =>
        from edge in new[] { CycleEdge.FuncWithArguments, CycleEdge.Func }
        from mode in new[] { "Auto", "On" }
        select new object[] { edge, mode };

    public static IEnumerable<object[]> PreviousCases() =>
        from edge in new[] { CycleEdge.FuncWithArguments, CycleEdge.FuncWithArgumentsByLet }
        from previous in new[] { 1, 3 }
        from mode in new[] { "Auto", "On" }
        select new object[] { edge, previous, mode };

    // Each screen opens the next two screens through lambdas of the same kind.
    [Theory]
    [MemberData(nameof(EdgeCases))]
    public Task ShouldGrowLinearlyForScreensOpeningTwoScreens(CycleEdge edge, string lifetime, string mode) =>
        CheckGrowth(Sizes, size => new Ring(size, edge, lifetime, mode));

    // The screens are disposable and are opened as Owned<T>, so their code is created under accumulators.
    [Theory]
    [MemberData(nameof(OwnedCases))]
    public Task ShouldGrowLinearlyForOwnedScreens(CycleEdge edge, string mode) =>
        CheckGrowth(Sizes, size => new Ring(size, edge, "Transient", mode) { IsOwned = true });

    // Each screen has a header and a body that share a transient service, and the header and the service
    // read the navigation that the delegate of the screen overrides.
    [Theory]
    [MemberData(nameof(ArgumentCases))]
    public Task ShouldGrowLinearlyForScreensWithOverrideReadingDiamond(CycleEdge edge, string mode) =>
        CheckGrowth(Sizes, size => new Ring(size, edge, "Transient", mode) { HasDiamond = true });

    // Each screen also reads the contexts of the previous screens of the ring. The argument of a delegate stays
    // in the scope of the screens below it, so a screen is reached in a different override scope on each path,
    // and it reads the value of the nearest screen above it on the path, which can differ on each visit.
    // This is the shape of the real application of #192, where the override rewrite did not complete.
    [Theory]
    [MemberData(nameof(PreviousCases))]
    public Task ShouldGrowLinearlyForScreensReadingContextsOfPreviousScreens(CycleEdge edge, int previous, string mode) =>
        CheckGrowth(Sizes, size => new Ring(size, edge, "Transient", mode) { Previous = previous });

    // A screen reads the context of the screen above it, which each pass of the cycle overrides again.
    // The lambda that opens the screen was created on the first pass, so it read the context of that pass.
    [Theory]
    [InlineData(1, "Auto")]
    [InlineData(1, "On")]
    [InlineData(3, "Auto")]
    public async Task ShouldReadContextsOfCurrentPassOfCycle(int previous, string mode)
    {
        // Given
        var ring = new Ring(6, CycleEdge.FuncWithArguments, "Transient", mode) { Previous = previous };

        // When
        var result = await ring.Create().RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe([ring.GetExpectedPath()], result);
    }

    // Each screen opens only the next one, so it reaches itself by a single lambda,
    // but a hub opens each screen of the ring, so the ring is entered in as many places as it has screens.
    [Theory]
    [MemberData(nameof(ArgumentCases))]
    public Task ShouldGrowLinearlyForHubOpeningEachScreenOfRing(CycleEdge edge, string mode) =>
        CheckGrowth(HubSizes, size => new Ring(size, edge, "Transient", mode) { Next = 1, HasHub = true });

    private async Task CheckGrowth(int[] sizes, Func<int, Ring> createRing)
    {
        // All the sizes are measured before the checks, so a failure shows the growth too.
        var measurements = new List<Measurement>();
        var problems = new List<string>();
        foreach (var size in sizes)
        {
            var ring = createRing(size);
            var stopwatch = Stopwatch.StartNew();
            var run = ring.Create().RunAsync(new Options(Options.ModernLanguageVersion));
            if (await Task.WhenAny(run, Task.Delay(GenerationTimeout)) != run)
            {
                throw new TimeoutException($"{size} screens are not generated in {GenerationTimeout}; previous: {string.Join("; ", measurements)}");
            }

            var result = await run;
            stopwatch.Stop();
            result.Errors.ShouldBeEmpty(result);
            measurements.Add(Measure(size, result, stopwatch.Elapsed));
            var expected = ring.GetExpectedPath();
            if (result.StdOut.Length != 1 || result.StdOut[0] != expected)
            {
                problems.Add($"{size} screens navigate{Environment.NewLine}  as {string.Join(Environment.NewLine, result.StdOut)}{Environment.NewLine}  instead of {expected}");
            }
        }

        var tokensRatio = GrowthRatio(measurements.Select(i => i.Tokens).ToArray());
        if (tokensRatio >= MaxGrowthRatio)
        {
            problems.Add($"Tokens grow by the ratio {tokensRatio:F2}");
        }

        var constructionsRatio = GrowthRatio(measurements.Select(i => i.Constructions).ToArray());
        if (constructionsRatio >= MaxGrowthRatio)
        {
            problems.Add($"Constructions of screens grow by the ratio {constructionsRatio:F2}");
        }

        problems.ShouldBeEmpty(string.Join(Environment.NewLine, measurements));
    }

    private Measurement Measure(int size, Result result, TimeSpan elapsed)
    {
        var namespaces = CSharpSyntaxTree.ParseText(result.GeneratedCode).GetRoot().DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>().Where(i => i.Name.ToString() == "Sample").ToArray();
        namespaces.ShouldNotBeEmpty(result);
        var constructions = namespaces.SelectMany(i => i.DescendantNodes()).OfType<ObjectCreationExpressionSyntax>()
            .Count(i => i.Type.ToString().Contains("Screen"));
        var measurement = new Measurement(
            size,
            namespaces.Sum(i => i.DescendantTokens().Count()),
            constructions,
            result.Warnings.Count(i => i.Id == LogId.WarningRootCodeIsTooLarge),
            elapsed);
        output.WriteLine(measurement.ToString());
        return measurement;
    }

    private static double GrowthRatio(IReadOnlyList<int> sizes)
    {
        var firstStep = sizes[1] - sizes[0];
        var lastStep = sizes[2] - sizes[1];
        return firstStep <= 0 ? lastStep <= 0 ? 1 : double.PositiveInfinity : (double)lastStep / firstStep;
    }

    private sealed record Measurement(int Size, int Tokens, int Constructions, int SizeWarnings, TimeSpan Elapsed)
    {
        public override string ToString() =>
            $"screens={Size} tokens={Tokens} screenConstructions={Constructions} DIW016={SizeWarnings} time={Elapsed.TotalSeconds:F1}s";
    }

    private sealed record Ring(int Size, CycleEdge Edge, string Lifetime, string Mode)
    {
        // Each screen opens the screens from 1 to this number of screens ahead.
        public int Next { get; init; } = 2;

        public bool IsOwned { get; init; }

        public bool HasDiamond { get; init; }

        // The number of the previous screens of the ring whose contexts each screen reads.
        public int Previous { get; init; }

        public bool HasHub { get; init; }

        private bool HasArguments => Edge is CycleEdge.FuncWithArguments or CycleEdge.FuncWithArgumentsByLet;

        private bool IsDeep => Edge == CycleEdge.FuncWithArguments;

        private string Item(int screen) => IsOwned ? $"Owned<Screen{screen}>" : $"Screen{screen}";

        private string Open(int screen) => Edge switch
        {
            CycleEdge.Func => $"Func<{Item(screen)}>",
            CycleEdge.Lazy => $"Lazy<{Item(screen)}>",
            _ => $"Func<INavigation, Context{screen}, {Item(screen)}>"
        };

        private string Call(int screen, string open, string step)
        {
            var call = Edge switch
            {
                CycleEdge.Func => $"{open}()",
                CycleEdge.Lazy => $"{open}.Value",
                _ => $"{open}(new Navigation({step} + 1), new Context{screen}($\"c{{{step}}}\"))"
            };
            return IsOwned ? $"{call}.Value" : call;
        }

        private int Target(int screen, int index) => (screen + 1 + index) % Size;

        public string Create()
        {
            var code = new StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine("using Pure.DI;");
            code.AppendLine("namespace Sample");
            code.AppendLine("{");
            code.AppendLine("    interface INavigation { }");
            code.AppendLine("    sealed class Navigation : INavigation { private readonly int _id; public Navigation(int id) => _id = id; public override string ToString() => $\"N{_id}\"; }");
            code.AppendLine("    interface IScreen { string Run(int step, int steps); }");
            if (HasDiamond)
            {
                code.AppendLine("    sealed class Services { public Services(INavigation navigation) => Navigation = navigation; public INavigation Navigation { get; } }");
                code.AppendLine("    sealed class Header { public Header(INavigation navigation, Services services) => Value = $\"{navigation}{services.Navigation}\"; public string Value { get; } }");
                code.AppendLine("    sealed class Body { public Body(Services services) => Value = $\"{services.Navigation}\"; public string Value { get; } }");
            }

            for (var screen = 0; screen < Size; screen++)
            {
                AppendScreen(code, screen);
            }

            if (HasHub)
            {
                var parameters = string.Join(", ", Enumerable.Range(0, Size).Select(i => $"{Open(i)} open{i}"));
                code.AppendLine($"    sealed class Hub {{ public Hub({parameters}) => Open0 = open0; public {Open(0)} Open0 {{ get; }} }}");
            }

            code.AppendLine("    partial class Composition");
            code.AppendLine("    {");
            code.AppendLine("        private static void Setup() => DI.Setup(nameof(Composition))");
            code.AppendLine($"            .Hint(Hint.LocalFunctions, \"{Mode}\")");
            if (Lifetime != "Transient")
            {
                for (var screen = 0; screen < Size; screen++)
                {
                    code.AppendLine($"            .Bind().As(Lifetime.{Lifetime}).To<Screen{screen}>()");
                }
            }

            if (Edge == CycleEdge.FuncWithArgumentsByLet)
            {
                code.AppendLine("            .Bind<Func<TT1, TT2, TT>>().As(Lifetime.PerBlock).To(ctx => new Func<TT1, TT2, TT>((TT1 arg1, TT2 arg2) => { ctx.Let<TT1>(arg1); ctx.Let<TT2>(arg2); ctx.Inject<TT>(ctx.Tag, out var value); return value; }))");
            }

            if (HasArguments && (HasDiamond || Previous > 0))
            {
                // The values that a level does not get from a delegate.
                code.AppendLine("            .Bind<INavigation>().To(_ => new Navigation(-1))");
                for (var screen = 0; screen < Size; screen++)
                {
                    code.AppendLine($"            .Bind<Context{screen}>().To(_ => new Context{screen}(\"default\"))");
                }
            }

            code.AppendLine(HasHub
                ? "            .Root<Hub>(\"Root\");"
                : $"            .Root<{Open(0)}>(\"Root\");");
            code.AppendLine("    }");
            var root = HasHub ? "new Composition().Root.Open0" : "new Composition().Root";
            code.AppendLine($"    public static class Program {{ public static void Main() => Console.WriteLine({Call(0, root, "-1").Replace("new Context0($\"c{-1}\")", "new Context0(\"start\")")}.Run(0, {Steps})); }}");
            code.AppendLine("}");
            return code.ToString();
        }

        private void AppendScreen(StringBuilder code, int screen)
        {
            var parameters = new List<string>();
            var fields = new StringBuilder();
            var assignments = new StringBuilder();
            var state = new List<string>();
            if (HasArguments)
            {
                code.AppendLine($"    sealed class Context{screen} {{ public Context{screen}(string value) => Value = value; public string Value {{ get; }} }}");
                parameters.Add($"Context{screen} context");
                parameters.Add("INavigation navigation");
                state.Add("{_context.Value}");
                state.Add("{_navigation}");
                fields.Append($"private readonly Context{screen} _context; private readonly INavigation _navigation; ");
                assignments.Append("_context = context; _navigation = navigation; ");
                for (var distance = 1; distance <= Previous; distance++)
                {
                    var previous = (screen - distance + Size) % Size;
                    parameters.Add($"Context{previous} previous{distance}");
                    state.Add($"{{_previous{distance}.Value}}");
                    fields.Append($"private readonly Context{previous} _previous{distance}; ");
                    assignments.Append($"_previous{distance} = previous{distance}; ");
                }

                if (HasDiamond)
                {
                    parameters.Add("Header header");
                    parameters.Add("Body body");
                    state.Add("{_header.Value}{_body.Value}");
                    fields.Append("private readonly Header _header; private readonly Body _body; ");
                    assignments.Append("_header = header; _body = body; ");
                }
            }

            var next = new StringBuilder();
            for (var index = 0; index < Next; index++)
            {
                var target = Target(screen, index);
                parameters.Add($"{Open(target)} open{index}");
                fields.Append($"private readonly {Open(target)} _open{index}; ");
                assignments.Append($"_open{index} = open{index}; ");
                next.Append($"step % {Next} == {index} ? (IScreen){Call(target, $"_open{index}", "step")} : ");
            }

            next.Append("null!");
            var disposable = IsOwned ? ", IDisposable" : "";
            var dispose = IsOwned ? "public void Dispose() { }" : "";
            var current = state.Count == 0 ? $"S{screen}" : $"S{screen}({string.Join(",", state)})";
            code.AppendLine($$"""
                    sealed class Screen{{screen}} : IScreen{{disposable}}
                    {
                        {{fields}}
                        public Screen{{screen}}({{string.Join(", ", parameters)}}) { {{assignments}} }
                        public string Run(int step, int steps)
                        {
                            var current = $"{{current}}";
                            if (step == steps) return current;
                            var next = {{next}};
                            return current + " " + next.Run(step + 1, steps);
                        }
                        {{dispose}}
                    }
                """);
        }

        // Simulates the navigation: a delegate gets the values visible where it is created,
        // and the built-in Func<T1, T2, T> passes its arguments to all the levels below it.
        public string GetExpectedPath()
        {
            var path = new List<string>();
            var screen = 0;
            var scope = new Dictionary<int, string> { [0] = "start" };
            var navigation = "N0";
            for (var step = 0; step <= Steps; step++)
            {
                if (!HasArguments)
                {
                    path.Add($"S{screen}");
                }
                else
                {
                    var state = new List<string> { scope[screen], navigation };
                    for (var distance = 1; distance <= Previous; distance++)
                    {
                        var previous = (screen - distance + Size) % Size;
                        state.Add(IsDeep && scope.TryGetValue(previous, out var value) ? value : "default");
                    }

                    if (HasDiamond)
                    {
                        var deepNavigation = IsDeep ? navigation : "N-1";
                        state.Add($"{deepNavigation}{deepNavigation}{deepNavigation}");
                    }

                    path.Add($"S{screen}({string.Join(",", state)})");
                }

                var target = Target(screen, step % Next);
                scope = IsDeep ? new Dictionary<int, string>(scope) : [];
                scope[target] = $"c{step}";
                navigation = $"N{step + 1}";
                screen = target;
            }

            return string.Join(" ", path);
        }
    }

    public enum CycleEdge
    {
        // The built-in Func<T1, T2, T> that overrides its arguments for all the levels below it.
        FuncWithArguments,
        // A binding of Func<T1, T2, T> that passes its arguments by ctx.Let to the current level only.
        FuncWithArgumentsByLet,
        Func,
        Lazy
    }
}
