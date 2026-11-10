namespace Pure.DI.IntegrationTests;

using System.Text;
using Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;

/// <summary>
/// Probes the code growth of the cycles whose screens open each other by edges of different kinds, see #192:
/// Func&lt;T&gt;, Lazy&lt;T&gt;, Func&lt;T1, T&gt; that overrides a context, Func&lt;Task&lt;T&gt;&gt; and IAsyncEnumerable&lt;T&gt;,
/// generic screens that read a root argument, and rings nested in the screens of an outer ring.
/// Each shape is generated in three sizes, and equal steps in the number of screens must add equal amounts of code.
/// Each shape also walks the screens and checks the values each screen gets.
/// </summary>
[Trait("Category", "CodeSize")]
public class CycleShapesCodeGrowthTests(ITestOutputHelper output)
{
    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromSeconds(30);
    private const int Steps = 11;
    // Equal steps in the number of screens must add approximately equal amounts of code.
    private const double MaxGrowthRatio = 1.35;

    public static IEnumerable<object[]> Cases() =>
        from shape in new[] { Shape.MixedEdges, Shape.TaskEdges, Shape.NestedRings, Shape.Generic, Shape.GenericWithContext }
        from lifetime in new[] { "Transient", "PerBlock" }
        from mode in new[] { "Auto", "On" }
        select new object[] { shape, lifetime, mode };

    [Theory]
    [MemberData(nameof(Cases))]
    public Task ShouldGrowLinearlyForCycleShape(Shape shape, string lifetime, string mode)
    {
        int[] sizes = shape == Shape.NestedRings ? [3, 5, 7] : [4, 6, 8];
        return CheckGrowth(sizes, size => CreateGraph(shape, size), lifetime, mode, isQuadratic: false);
    }

    // Each screen gets IAsyncEnumerable<IScreen> of all the other screens, so each collection has an element for each of them,
    // and the code grows with the square of the number of screens, while the code for a screen must grow linearly.
    [Theory]
    [InlineData("Transient", "Auto")]
    [InlineData("Transient", "On")]
    [InlineData("PerBlock", "Auto")]
    [InlineData("PerBlock", "On")]
    [InlineData("Singleton", "Auto")]
    public Task ShouldGrowLinearlyForScreensGettingAllScreensAsynchronously(string lifetime, string mode) =>
        CheckGrowth([4, 6, 8], size => new Graph(Enumerable.Range(0, size).Select(i => new Screen($"S{i}", [])).ToArray(), IsAsyncEnumerable: true), lifetime, mode, isQuadratic: true);

    private async Task CheckGrowth(int[] sizes, Func<int, Graph> createGraph, string lifetime, string mode, bool isQuadratic)
    {
        // All the sizes are measured before the checks, so a failure shows the growth too.
        var measurements = new List<Measurement>();
        var problems = new List<string>();
        foreach (var size in sizes)
        {
            var graph = createGraph(size);
            Result result;
            try
            {
                result = await graph.Create(lifetime, mode).RunAsync(new Options(Options.ModernLanguageVersion), GenerationTimeout);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"{size} screens are not generated in {GenerationTimeout}; previous: {string.Join("; ", measurements)}");
            }

            result.Errors.ShouldBeEmpty(result);
            foreach (var warning in result.Warnings.Where(i => i.Id != LogId.WarningRootCodeIsTooLarge).Select(i => i.Id).Distinct())
            {
                problems.Add($"{size} screens are generated with the warning {warning}");
            }

            measurements.Add(Measure(size, graph.Screens.Length, result));
            var expected = graph.GetExpectedPath();
            if (result.StdOut.Length != 1 || result.StdOut[0] != expected)
            {
                problems.Add($"{size} screens navigate{Environment.NewLine}  as {string.Join(Environment.NewLine, result.StdOut)}{Environment.NewLine}  instead of {expected}");
            }
        }

        var tokensRatio = GrowthRatio(measurements.Select(i => isQuadratic ? i.Tokens / i.Screens : i.Tokens).ToArray());
        if (tokensRatio >= MaxGrowthRatio)
        {
            problems.Add($"Tokens grow by the ratio {tokensRatio:F2}");
        }

        var constructionsRatio = GrowthRatio(measurements.Select(i => i.Constructions).ToArray());
        if (constructionsRatio >= MaxGrowthRatio)
        {
            problems.Add($"Constructions of screens grow by the ratio {constructionsRatio:F2}");
        }

        if (measurements.Any(i => i.SizeWarnings > 0))
        {
            problems.Add("The code of a root is too large");
        }

        problems.ShouldBeEmpty(string.Join(Environment.NewLine, measurements));
    }

    private Measurement Measure(int size, int screens, Result result)
    {
        var namespaces = CSharpSyntaxTree.ParseText(result.GeneratedCode).GetRoot().DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>().Where(i => i.Name.ToString() == "Sample").ToArray();
        namespaces.ShouldNotBeEmpty(result);
        var constructions = namespaces.SelectMany(i => i.DescendantNodes()).OfType<ObjectCreationExpressionSyntax>()
            .Count(i => i.Type.ToString().Contains("Screen"));
        var measurement = new Measurement(
            size,
            screens,
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

    private static Graph CreateGraph(Shape shape, int size)
    {
        Screen Ring(int screen, params Edge[] edges) => new($"S{screen}", edges.Select((edge, index) => edge with { Target = (screen + 1 + index) % size }).ToArray());
        switch (shape)
        {
            case Shape.MixedEdges:
                return new Graph(Enumerable.Range(0, size).Select(i => Ring(i, new Edge(0, EdgeKind.Func), new Edge(0, EdgeKind.Lazy), new Edge(0, EdgeKind.FuncWithContext))).ToArray());

            case Shape.TaskEdges:
                return new Graph(Enumerable.Range(0, size).Select(i => Ring(i, new Edge(0, EdgeKind.Func), new Edge(0, EdgeKind.FuncOfTask))).ToArray());

            case Shape.Generic:
                return new Graph(Enumerable.Range(0, size).Select(i => Ring(i, new Edge(0, EdgeKind.Func), new Edge(0, EdgeKind.Func))).ToArray(), IsGeneric: true);

            case Shape.GenericWithContext:
                return new Graph(Enumerable.Range(0, size).Select(i => Ring(i, new Edge(0, EdgeKind.Func), new Edge(0, EdgeKind.FuncWithContext))).ToArray(), IsGeneric: true);

            default:
                // An outer screen opens the next outer screen and its inner ring of three screens, which leads back to it.
                const int inner = 3;
                var screens = new List<Screen>();
                for (var outer = 0; outer < size; outer++)
                {
                    var first = outer * (inner + 1);
                    screens.Add(new Screen($"O{outer}", [new Edge((outer + 1) % size * (inner + 1), EdgeKind.Func), new Edge(first + 1, EdgeKind.Func)]));
                    for (var index = 0; index < inner; index++)
                    {
                        screens.Add(new Screen($"I{outer}.{index}", [new Edge(first + 1 + (index + 1) % inner, EdgeKind.Func), new Edge(first, EdgeKind.Lazy)]));
                    }
                }

                return new Graph(screens.ToArray());
        }
    }

    private sealed record Measurement(int Size, int Screens, int Tokens, int Constructions, int SizeWarnings)
    {
        public override string ToString() =>
            $"size={Size} screens={Screens} tokens={Tokens} screenConstructions={Constructions} DIW016={SizeWarnings}";
    }

    public enum Shape
    {
        // Each screen opens the next screen by Func<T>, the one after it by Lazy<T> and the third by Func<T1, T> with a context.
        MixedEdges,
        // Each screen opens the next screen by Func<T> and the one after it by Func<Task<T>>.
        TaskEdges,
        // The rings of three screens in each screen of an outer ring.
        NestedRings,
        // Generic screens that read the argument of a generic root.
        Generic,
        // Generic screens that read the argument of a generic root and a context that Func<T1, T> overrides.
        GenericWithContext
    }

    private enum EdgeKind
    {
        Func,
        Lazy,
        FuncWithContext,
        FuncOfTask
    }

    private sealed record Edge(int Target, EdgeKind Kind);

    private sealed record Screen(string Label, Edge[] Edges);

    private sealed record Graph(Screen[] Screens, bool IsGeneric = false, bool IsAsyncEnumerable = false)
    {
        private bool HasContext => Screens.Any(screen => screen.Edges.Any(edge => edge.Kind == EdgeKind.FuncWithContext));

        private string Type(int screen) => IsGeneric ? $"Screen{screen}<T>" : $"Screen{screen}";

        private string Open(Edge edge) => edge.Kind switch
        {
            EdgeKind.Lazy => $"Lazy<{Type(edge.Target)}>",
            EdgeKind.FuncWithContext => $"Func<Context, {Type(edge.Target)}>",
            EdgeKind.FuncOfTask => $"Func<Task<{Type(edge.Target)}>>",
            _ => $"Func<{Type(edge.Target)}>"
        };

        private static string Call(Edge edge, string field) => edge.Kind switch
        {
            EdgeKind.Lazy => $"{field}.Value",
            EdgeKind.FuncWithContext => $"{field}(new Context(\"c\" + step))",
            EdgeKind.FuncOfTask => $"{field}().GetAwaiter().GetResult()",
            _ => $"{field}()"
        };

        public string Create(string lifetime, string mode)
        {
            var code = new StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using System.Threading.Tasks;");
            code.AppendLine("using Pure.DI;");
            code.AppendLine("namespace Sample");
            code.AppendLine("{");
            code.AppendLine("    interface IScreen { string Run(int step, int steps); }");
            code.AppendLine("    sealed class Context { public Context(string value) { Value = value; } public string Value { get; } }");
            code.AppendLine("""
                    static class Screens
                    {
                        public static IScreen Get(IAsyncEnumerable<IScreen> screens, int index)
                        {
                            var enumerator = screens.GetAsyncEnumerator();
                            try
                            {
                                for (var i = 0; ; i++)
                                {
                                    if (!enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult()) throw new InvalidOperationException();
                                    if (i == index) return enumerator.Current;
                                }
                            }
                            finally { enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
                        }
                    }
                """);
            var bindings = new StringBuilder();
            for (var screen = 0; screen < Screens.Length; screen++)
            {
                AppendScreen(code, screen);
                var contract = IsAsyncEnumerable ? "IScreen" : Type(screen).Replace("<T>", "<TT>");
                var implementation = Type(screen).Replace("<T>", "<TT>");
                var tags = IsAsyncEnumerable ? $"{screen}" : "";
                if (lifetime != "Transient" || IsAsyncEnumerable)
                {
                    bindings.AppendLine($"            .Bind<{contract}>({tags}).As(Lifetime.{lifetime}).To<{implementation}>()");
                }
            }

            if (HasContext)
            {
                bindings.AppendLine("            .Bind<Context>().To(_ => new Context(\"d\"))");
            }

            code.AppendLine("    partial class Composition");
            code.AppendLine("    {");
            code.AppendLine("        private static void Setup() => DI.Setup(nameof(Composition))");
            code.AppendLine($"            .Hint(Hint.LocalFunctions, \"{mode}\")");
            code.Append(bindings);
            if (IsGeneric)
            {
                // Resolve methods cannot supply the type arguments of a generic root.
                code.AppendLine("            .Hint(Hint.Resolve, \"Off\")");
                code.AppendLine("            .RootArg<TT>(\"value\")");
                code.AppendLine("            .Root<Screen0<TT>>(\"Get\");");
            }
            else
            {
                code.AppendLine(IsAsyncEnumerable ? "            .Root<IScreen>(\"Root\", 0);" : "            .Root<Screen0>(\"Root\");");
            }

            code.AppendLine("    }");
            var root = IsGeneric ? "new Composition().Get<int>(7)" : "new Composition().Root";
            code.AppendLine($"    public static class Program {{ public static void Main() => Console.WriteLine({root}.Run(0, {Steps})); }}");
            code.AppendLine("}");
            return code.ToString();
        }

        private void AppendScreen(StringBuilder code, int screen)
        {
            var edges = Screens[screen].Edges;
            var parameters = new List<string>();
            var fields = new StringBuilder();
            var assignments = new StringBuilder();
            var state = new List<string>();
            if (IsGeneric)
            {
                parameters.Add("T value");
                fields.Append("private readonly T _value; ");
                assignments.Append("_value = value; ");
                state.Add("{_value}");
            }

            if (HasContext)
            {
                parameters.Add("Context context");
                fields.Append("private readonly Context _context; ");
                assignments.Append("_context = context; ");
                state.Add("{_context.Value}");
            }

            string next;
            if (IsAsyncEnumerable)
            {
                parameters.Add("IAsyncEnumerable<IScreen> screens");
                fields.Append("private readonly IAsyncEnumerable<IScreen> _screens; ");
                assignments.Append("_screens = screens; ");
                next = "Screens.Get(_screens, step % 2)";
            }
            else
            {
                var expression = new StringBuilder();
                for (var index = 0; index < edges.Length; index++)
                {
                    parameters.Add($"{Open(edges[index])} open{index}");
                    fields.Append($"private readonly {Open(edges[index])} _open{index}; ");
                    assignments.Append($"_open{index} = open{index}; ");
                    expression.Append($"step % {edges.Length} == {index} ? (IScreen){Call(edges[index], $"_open{index}")} : ");
                }

                expression.Append("null!");
                next = expression.ToString();
            }

            var label = Screens[screen].Label;
            var current = state.Count == 0 ? label : $"{label}({string.Join(",", state)})";
            code.AppendLine($$"""
                    sealed class {{Type(screen)}} : IScreen
                    {
                        {{fields}}
                        public Screen{{screen}}({{string.Join(", ", parameters)}}) { {{assignments}} }
                        public string Run(int step, int steps)
                        {
                            var current = $"{{current}}";
                            if (step == steps) return current;
                            IScreen next = {{next}};
                            return current + " " + next.Run(step + 1, steps);
                        }
                    }
                """);
        }

        // Simulates the walk: a screen reads the context of the nearest Func<T1, T> on the path to it.
        public string GetExpectedPath()
        {
            var path = new List<string>();
            var screen = 0;
            var context = "d";
            for (var step = 0; step <= Steps; step++)
            {
                var state = new List<string>();
                if (IsGeneric)
                {
                    state.Add("7");
                }

                if (HasContext)
                {
                    state.Add(context);
                }

                var label = Screens[screen].Label;
                path.Add(state.Count == 0 ? label : $"{label}({string.Join(",", state)})");
                if (IsAsyncEnumerable)
                {
                    // A screen does not get itself as an element of its collection.
                    screen = Enumerable.Range(0, Screens.Length).Where(i => i != screen).ElementAt(step % 2);
                    continue;
                }

                var edge = Screens[screen].Edges[step % Screens[screen].Edges.Length];
                if (edge.Kind == EdgeKind.FuncWithContext)
                {
                    context = $"c{step}";
                }

                screen = edge.Target;
            }

            return string.Join(" ", path);
        }
    }
}
