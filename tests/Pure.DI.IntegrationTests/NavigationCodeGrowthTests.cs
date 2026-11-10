namespace Pure.DI.IntegrationTests;

using System.Diagnostics;
using System.Text;
using Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;

/// <summary>
/// Probes the code growth of the navigation graphs that <see cref="CycleCodeGrowthTests"/> does not cover, see #192.
/// There the screens of a ring are transient or PerBlock, are created by their constructors and open each other directly.
/// Here the screens also open each other in both directions, through a router or through a collection of all the screens,
/// are shared, are created by factories or by the injection of members, and read the values overridden outside the cycle.
/// Each scenario is generated in three sizes, and equal steps in the number of screens must add equal amounts of code.
/// Each scenario also navigates along the cycles and checks the values each screen gets.
/// </summary>
[Trait("Category", "CodeSize")]
public class NavigationCodeGrowthTests(ITestOutputHelper output)
{
    private static readonly int[] Sizes = [4, 6, 8];
    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromSeconds(30);
    private const int Steps = 9;
    // Equal steps in the number of screens must add approximately equal amounts of code.
    private const double MaxGrowthRatio = 1.35;

    private static readonly Edge[] Edges = [Edge.Func, Edge.Lazy, Edge.FuncWithArgument, Edge.FuncOfOwned];
    private static readonly string[] Modes = ["Auto", "On"];

    private static IEnumerable<object[]> Cases(IEnumerable<Scenario> scenarios, bool knownProblems) =>
        from scenario in scenarios
        from mode in Modes
        where GetKnownProblem(scenario) is not null == knownProblems
        select new object[] { scenario with { Mode = mode } };

    // The scenarios whose code still grows with the number of paths through the graph, with the reason of the growth.
    // They are not checked until the growth is fixed, see ShouldGrowLinearlyForKnownProblems.
    private static string? GetKnownProblem(Scenario scenario) => scenario switch
    {
        { Topology: Topology.Router, RouterLifetime: "PerBlock", Edge: not Edge.FuncOfOwned } =>
            "Each lambda creates a router of its own with the lambdas for all the screens, so the code grows with the square of the number of screens.",
        { Edge: Edge.Lazy or Edge.FuncOfOwned, Lifetime: "PerResolve" or "Singleton" or "Scoped" } =>
            "A shared screen under Lazy<T> or Owned<T> has a single use site, so it is created in place in each lambda.",
        { HasTags: true } =>
            "A transient instance gets a local function for each tag of its injections, although its code does not depend on the tag.",
        { Topology: Topology.Ring, Edge: Edge.FuncOfOwned, Lifetime: "PerBlock" } =>
            "A PerBlock screen under Owned<T> is not the result of a lambda, so it is created in place in each lambda.",
        { Edge: Edge.FuncOfOwned, Creation: Creation.Factory or Creation.FactoryWithOverride } =>
            "A screen created by a factory under Owned<T> is created in place in each lambda.",
        _ => null
    };

    public static IEnumerable<object[]> KnownProblemCases() =>
        BackCases(true)
            .Concat(RouterCases(true))
            .Concat(EnumerableCases(true))
            .Concat(SharedCases(true))
            .Concat(CreationCases(true))
            .Concat(SessionCases(true))
            .Concat(ServiceCases(true))
            .Concat(TagCases(true))
            .Concat(RootCases(true));

    // Each screen opens the next screen and the previous one, so each pair of neighbors is a cycle of its own.
    public static IEnumerable<object[]> BackCases() => BackCases(false);

    private static IEnumerable<object[]> BackCases(bool knownProblems) => Cases(
        from edge in Edges
        from lifetime in new[] { "Transient", "PerBlock" }
        select new Scenario { Topology = Topology.Back, Edge = edge, Lifetime = lifetime },
        knownProblems);

    // Each screen gets a router that opens each screen, so all the cycles pass through one node that is not a lambda.
    public static IEnumerable<object[]> RouterCases() => RouterCases(false);

    private static IEnumerable<object[]> RouterCases(bool knownProblems) => Cases(
        from edge in Edges
        from routerLifetime in new[] { "Transient", "PerBlock", "PerResolve", "Singleton" }
        select new Scenario { Topology = Topology.Router, Edge = edge, RouterLifetime = routerLifetime },
        knownProblems);

    // Each screen gets the lazy collection of all the other screens. Each collection has an element for each of them,
    // so the code grows with the square of the number of screens, and the code for a screen must grow linearly.
    public static IEnumerable<object[]> EnumerableCases() => EnumerableCases(false);

    private static IEnumerable<object[]> EnumerableCases(bool knownProblems) => Cases(
        from lifetime in new[] { "Transient", "PerBlock", "PerResolve", "Singleton" }
        from creation in new[] { Creation.Constructor, Creation.Factory }
        select new Scenario { Topology = Topology.Enumerable, Lifetime = lifetime, Creation = creation },
        knownProblems);

    // The screens are shared, so a lambda returns the instance created by the code of another lambda.
    public static IEnumerable<object[]> SharedCases() => SharedCases(false);

    private static IEnumerable<object[]> SharedCases(bool knownProblems) => Cases(
        from edge in new[] { Edge.Func, Edge.Lazy, Edge.FuncOfOwned }
        from lifetime in new[] { "PerResolve", "Singleton", "Scoped" }
        select new Scenario { Edge = edge, Lifetime = lifetime },
        knownProblems);

    // The screens are created by factories or get the lambdas by their members.
    public static IEnumerable<object[]> CreationCases() => CreationCases(false);

    private static IEnumerable<object[]> CreationCases(bool knownProblems) => Cases(
        from creation in new[] { Creation.Constructor, Creation.Property, Creation.Method, Creation.Factory, Creation.FactoryWithOverride }
        from edge in Edges
        from lifetime in new[] { "Transient", "PerBlock" }
        // CycleCodeGrowthTests covers the other screens created by their constructors.
        where creation != Creation.Constructor || edge == Edge.FuncOfOwned && lifetime == "PerBlock"
        select new Scenario { Edge = edge, Lifetime = lifetime, Creation = creation },
        knownProblems);

    // The root delegate overrides a session outside the cycle, and each screen reads it, directly and through a service.
    public static IEnumerable<object[]> SessionCases() => SessionCases(false);

    private static IEnumerable<object[]> SessionCases(bool knownProblems) => Cases(
        from topology in new[] { Topology.Ring, Topology.Router }
        from edge in Edges
        from serviceLifetime in new[] { null, "Transient", "PerBlock" }
        select new Scenario { Topology = topology, Edge = edge, HasSession = true, ServiceLifetime = serviceLifetime },
        knownProblems);

    // Each screen gets a service that does not depend on the cycle.
    public static IEnumerable<object[]> ServiceCases() => ServiceCases(false);

    private static IEnumerable<object[]> ServiceCases(bool knownProblems) => Cases(
        from edge in Edges
        from serviceLifetime in new[] { "Transient", "PerBlock", "PerResolve", "Singleton" }
        select new Scenario { Edge = edge, ServiceLifetime = serviceLifetime },
        knownProblems);

    // Each screen opens the next screens by different tags, and the code of a transient instance depends on its tag.
    private static IEnumerable<object[]> TagCases(bool knownProblems) => Cases(
        from edge in Edges
        from lifetime in new[] { "Transient", "PerBlock" }
        select new Scenario { Edge = edge, Lifetime = lifetime, HasTags = true },
        knownProblems);

    // Several roots enter the same ring, and each root has the code of its own.
    public static IEnumerable<object[]> RootCases() => RootCases(false);

    private static IEnumerable<object[]> RootCases(bool knownProblems) => Cases(
        from edge in Edges
        select new Scenario { Edge = edge, Roots = 3 },
        knownProblems);

    [Theory]
    [MemberData(nameof(BackCases))]
    public Task ShouldGrowLinearlyForScreensOpeningNextAndPreviousScreens(Scenario scenario) => CheckGrowth(scenario);

    [Theory]
    [MemberData(nameof(RouterCases))]
    public Task ShouldGrowLinearlyForScreensOpeningEachOtherThroughRouter(Scenario scenario) => CheckGrowth(scenario);

    [Theory]
    [MemberData(nameof(EnumerableCases))]
    public Task ShouldGrowLinearlyForScreensGettingAllScreens(Scenario scenario) => CheckGrowth(scenario);

    [Theory]
    [MemberData(nameof(SharedCases))]
    public Task ShouldGrowLinearlyForSharedScreens(Scenario scenario) => CheckGrowth(scenario);

    [Theory]
    [MemberData(nameof(CreationCases))]
    public Task ShouldGrowLinearlyForScreensCreatedByFactoriesOrMembers(Scenario scenario) => CheckGrowth(scenario);

    [Theory]
    [MemberData(nameof(SessionCases))]
    public Task ShouldGrowLinearlyForScreensReadingValueOverriddenOutsideCycle(Scenario scenario) => CheckGrowth(scenario);

    [Theory]
    [MemberData(nameof(ServiceCases))]
    public Task ShouldGrowLinearlyForScreensWithService(Scenario scenario) => CheckGrowth(scenario);

    [Theory]
    [MemberData(nameof(RootCases))]
    public Task ShouldGrowLinearlyForSeveralRootsOfRing(Scenario scenario) => CheckGrowth(scenario);

    // Remove the Skip to see how the code of these scenarios grows.
    [Theory(Skip = "The code of these scenarios still grows with the number of paths through the graph, see GetKnownProblem.")]
    [MemberData(nameof(KnownProblemCases))]
    public Task ShouldGrowLinearlyForKnownProblems(Scenario scenario) => CheckGrowth(scenario);

    private async Task CheckGrowth(Scenario scenario)
    {
        // All the sizes are measured before the checks, so a failure shows the growth too.
        var measurements = new List<Measurement>();
        var problems = new List<string>();
        foreach (var size in Sizes)
        {
            var stopwatch = Stopwatch.StartNew();
            Result result;
            try
            {
                result = await scenario.Create(size).RunAsync(new Options(Options.ModernLanguageVersion), GenerationTimeout);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"{size} screens are not generated in {GenerationTimeout}; previous: {string.Join("; ", measurements)}");
            }

            stopwatch.Stop();
            result.Errors.ShouldBeEmpty(result);
            foreach (var warning in result.Warnings.Where(i => i.Id != LogId.WarningRootCodeIsTooLarge).Select(i => i.Id).Distinct())
            {
                problems.Add($"{size} screens are generated with the warning {warning}");
            }

            measurements.Add(Measure(size, result, stopwatch.Elapsed));
            var expected = scenario.GetExpectedPath(size);
            if (result.StdOut.Length != 1 || result.StdOut[0] != expected)
            {
                problems.Add($"{size} screens navigate{Environment.NewLine}  as {string.Join(Environment.NewLine, result.StdOut)}{Environment.NewLine}  instead of {expected}");
            }
        }

        var tokensRatio = GrowthRatio(measurements.Select(i => scenario.IsQuadratic ? i.Tokens / i.Size : i.Tokens).ToArray());
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

    public enum Topology
    {
        // Each screen opens the next two screens.
        Ring,
        // Each screen opens the next screen and the previous one.
        Back,
        // Each screen gets a router that has a lambda for each screen.
        Router,
        // Each screen gets IEnumerable<IScreen> of all the other screens.
        Enumerable
    }

    public enum Edge
    {
        Func,
        Lazy,
        // The built-in Func<T1, T> that overrides its argument, which the screen it creates reads.
        FuncWithArgument,
        // Func<Owned<T>> of a disposable screen, whose code is created under an accumulator.
        FuncOfOwned
    }

    public enum Creation
    {
        Constructor,
        Property,
        Method,
        // A factory that injects each dependency by ctx.Inject.
        Factory,
        // A factory on the cycle that overrides a title, which a dependency of the screen reads.
        FactoryWithOverride
    }

    // A record with public properties is shown in the name of a test case and is serialized by its properties.
    public sealed record Scenario : IXunitSerializable
    {
        public Topology Topology { get; set; } = Topology.Ring;

        public Edge Edge { get; set; } = Edge.Func;

        public string Lifetime { get; set; } = "Transient";

        public Creation Creation { get; set; } = Creation.Constructor;

        public string Mode { get; set; } = "Auto";

        // The root is a delegate with a session that each screen reads.
        public bool HasSession { get; set; }

        // The lifetime of a service that each screen gets. A transient or PerBlock one reads the session.
        public string? ServiceLifetime { get; set; }

        public string RouterLifetime { get; set; } = "Transient";

        public bool HasTags { get; set; }

        public int Roots { get; set; } = 1;

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Topology), Topology.ToString());
            info.AddValue(nameof(Edge), Edge.ToString());
            info.AddValue(nameof(Lifetime), Lifetime);
            info.AddValue(nameof(Creation), Creation.ToString());
            info.AddValue(nameof(Mode), Mode);
            info.AddValue(nameof(HasSession), HasSession);
            info.AddValue(nameof(ServiceLifetime), ServiceLifetime);
            info.AddValue(nameof(RouterLifetime), RouterLifetime);
            info.AddValue(nameof(HasTags), HasTags);
            info.AddValue(nameof(Roots), Roots);
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Topology = Enum.Parse<Topology>(info.GetValue<string>(nameof(Topology)));
            Edge = Enum.Parse<Edge>(info.GetValue<string>(nameof(Edge)));
            Lifetime = info.GetValue<string>(nameof(Lifetime));
            Creation = Enum.Parse<Creation>(info.GetValue<string>(nameof(Creation)));
            Mode = info.GetValue<string>(nameof(Mode));
            HasSession = info.GetValue<bool>(nameof(HasSession));
            ServiceLifetime = info.GetValue<string?>(nameof(ServiceLifetime));
            RouterLifetime = info.GetValue<string>(nameof(RouterLifetime));
            HasTags = info.GetValue<bool>(nameof(HasTags));
            Roots = info.GetValue<int>(nameof(Roots));
        }

        // Each screen depends on each of the other screens.
        public bool IsQuadratic => Topology == Topology.Enumerable;

        private bool HasArgument => Edge == Edge.FuncWithArgument && Topology != Topology.Enumerable;

        private bool IsOwned => Edge == Edge.FuncOfOwned && Topology != Topology.Enumerable;

        private bool IsFactory => Creation is Creation.Factory or Creation.FactoryWithOverride;

        private bool ServiceReadsSession => HasSession && ServiceLifetime is "Transient" or "PerBlock";

        private string Item(int screen) => IsOwned ? $"Owned<Screen{screen}>" : $"Screen{screen}";

        private string Open(int screen) => Edge switch
        {
            Edge.Lazy => $"Lazy<{Item(screen)}>",
            Edge.FuncWithArgument => $"Func<Context{screen}, {Item(screen)}>",
            _ => $"Func<{Item(screen)}>"
        };

        private string Call(int screen, string open, string step)
        {
            var call = Edge switch
            {
                Edge.Lazy => $"{open}.Value",
                Edge.FuncWithArgument => $"{open}(new Context{screen}($\"c{{{step}}}\"))",
                _ => $"{open}()"
            };
            return IsOwned ? $"{call}.Value" : call;
        }

        private int[] Targets(int size, int screen) => Topology switch
        {
            Topology.Back => [(screen + 1) % size, (screen - 1 + size) % size],
            // A screen does not get itself as an element of its collection.
            Topology.Enumerable => Enumerable.Range(0, size).Where(i => i != screen).Take(2).ToArray(),
            _ => [(screen + 1) % size, (screen + 2) % size]
        };

        private string RootType
        {
            get
            {
                if (Topology == Topology.Enumerable)
                {
                    return HasSession ? "Func<Session, IScreen>" : "IScreen";
                }

                if (!HasSession)
                {
                    return Open(0);
                }

                return HasArgument ? $"Func<Session, Context0, {Item(0)}>" : $"Func<Session, {Item(0)}>";
            }
        }

        private string RootCall(string root)
        {
            if (Topology == Topology.Enumerable)
            {
                return HasSession ? $"{root}(new Session(\"session\"))" : root;
            }

            string call;
            if (HasSession)
            {
                call = HasArgument ? $"{root}(new Session(\"session\"), new Context0(\"start\"))" : $"{root}(new Session(\"session\"))";
            }
            else
            {
                call = Edge switch
                {
                    Edge.Lazy => $"{root}.Value",
                    Edge.FuncWithArgument => $"{root}(new Context0(\"start\"))",
                    _ => $"{root}()"
                };
            }

            return IsOwned ? $"{call}.Value" : call;
        }

        public string Create(int size)
        {
            var code = new StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine("using System.Linq;");
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using Pure.DI;");
            code.AppendLine("namespace Sample");
            code.AppendLine("{");
            code.AppendLine("    interface IScreen { string Run(int step, int steps); }");
            code.AppendLine("    sealed class Session { public Session(string value) => Value = value; public string Value { get; } }");
            code.AppendLine("    sealed class Title { public Title(string value) => Value = value; public string Value { get; } }");
            code.AppendLine("    sealed class Header { public Header(Title title) => Title = title; public Title Title { get; } }");
            code.AppendLine(ServiceReadsSession
                ? "    sealed class Service { public Service(Session session) => Value = session.Value; public string Value { get; } }"
                : "    sealed class Service { public string Value => \"v\"; }");

            for (var screen = 0; screen < size; screen++)
            {
                if (HasArgument)
                {
                    code.AppendLine($"    sealed class Context{screen} {{ public Context{screen}(string value) => Value = value; public string Value {{ get; }} }}");
                }
            }

            var bindings = new StringBuilder();
            for (var screen = 0; screen < size; screen++)
            {
                AppendScreen(code, bindings, size, screen);
            }

            if (Topology == Topology.Router)
            {
                var parameters = string.Join(", ", Enumerable.Range(0, size).Select(i => $"{Open(i)} open{i}"));
                var assignments = string.Join(" ", Enumerable.Range(0, size).Select(i => $"_open{i} = open{i};"));
                var fields = string.Join(" ", Enumerable.Range(0, size).Select(i => $"private readonly {Open(i)} _open{i};"));
                var cases = string.Join(" ", Enumerable.Range(0, size).Select(i => $"case {i}: return {Call(i, $"_open{i}", "step")};"));
                code.AppendLine($$"""
                        sealed class Router
                        {
                            {{fields}}
                            public Router({{parameters}}) { {{assignments}} }
                            public IScreen Open(int index, int step)
                            {
                                switch (index) { {{cases}} default: throw new ArgumentOutOfRangeException(nameof(index)); }
                            }
                        }
                    """);
                bindings.AppendLine($"            .Bind<Router>().As(Lifetime.{RouterLifetime}).To<Router>()");
            }

            if (ServiceLifetime is not null)
            {
                bindings.AppendLine($"            .Bind<Service>().As(Lifetime.{ServiceLifetime}).To<Service>()");
            }

            if (Creation == Creation.FactoryWithOverride)
            {
                // The title that a level does not get from a factory.
                bindings.AppendLine("            .Bind<Title>().To(_ => new Title(\"default\"))");
            }

            code.AppendLine("    partial class Composition");
            code.AppendLine("    {");
            code.AppendLine("        private static void Setup() => DI.Setup(nameof(Composition))");
            code.AppendLine($"            .Hint(Hint.LocalFunctions, \"{Mode}\")");
            code.Append(bindings);
            var tag = Topology == Topology.Enumerable ? ", 0" : HasTags ? ", \"a\"" : "";
            code.AppendLine($"            .Root<{RootType}>(\"Root\"{tag})");
            for (var root = 1; root < Roots; root++)
            {
                code.AppendLine($"            .Root<{Open(root)}>(\"Root{root}\"{tag})");
            }

            code.AppendLine("            ;");
            code.AppendLine("    }");
            code.AppendLine($"    public static class Program {{ public static void Main() => Console.WriteLine({RootCall("new Composition().Root")}.Run(0, {Steps})); }}");
            code.AppendLine("}");
            return code.ToString();
        }

        private void AppendScreen(StringBuilder code, StringBuilder bindings, int size, int screen)
        {
            // The type, the name and the tag of each dependency.
            var dependencies = new List<(string Type, string Name, string? Tag)>();
            var state = new List<string>();
            if (HasArgument)
            {
                dependencies.Add(($"Context{screen}", "Context", null));
                state.Add("{Context.Value}");
            }

            if (HasSession)
            {
                dependencies.Add(("Session", "Session", null));
                state.Add("{Session.Value}");
            }

            if (Creation == Creation.FactoryWithOverride)
            {
                dependencies.Add(("Header", "Header", null));
                state.Add("{Header.Title.Value}");
            }

            if (ServiceLifetime is not null)
            {
                dependencies.Add(("Service", "Service", null));
                state.Add("{Service.Value}");
            }

            string next;
            switch (Topology)
            {
                case Topology.Router:
                    dependencies.Add(("Router", "Router", null));
                    next = $"Router.Open(({screen} + 1 + step % 2) % {size}, step)";
                    break;

                case Topology.Enumerable:
                    dependencies.Add(("IEnumerable<IScreen>", "Screens", null));
                    next = "Screens.Skip(step % 2).First()";
                    break;

                default:
                    var targets = Targets(size, screen);
                    var expression = new StringBuilder();
                    for (var index = 0; index < targets.Length; index++)
                    {
                        dependencies.Add((Open(targets[index]), $"Open{index}", HasTags ? index == 0 ? "a" : "b" : null));
                        expression.Append($"step % {targets.Length} == {index} ? (IScreen){Call(targets[index], $"Open{index}", "step")} : ");
                    }

                    expression.Append("null!");
                    next = expression.ToString();
                    break;
            }

            var members = new StringBuilder();
            for (var index = 0; index < dependencies.Count; index++)
            {
                var (type, name, _) = dependencies[index];
                var attribute = Creation == Creation.Property ? $"[Ordinal({index})] " : "";
                members.Append($"{attribute}public {type} {name} {{ get; set; }} = default!; ");
            }

            var parameters = string.Join(", ", dependencies.Select(i => $"{(i.Tag is null ? "" : $"[Tag(\"{i.Tag}\")] ")}{i.Type} {ToParameter(i.Name)}"));
            var assignments = string.Join(" ", dependencies.Select(i => $"{i.Name} = {ToParameter(i.Name)};"));
            var initializer = Creation switch
            {
                Creation.Property => "",
                Creation.Method => $"[Ordinal(0)] public void Initialize({parameters}) {{ {assignments} }}",
                _ => $"public Screen{screen}({parameters}) {{ {assignments} }}"
            };

            var disposable = IsOwned ? ", IDisposable" : "";
            var dispose = IsOwned ? "public void Dispose() { }" : "";
            var current = state.Count == 0 ? $"S{screen}" : $"S{screen}({string.Join(",", state)})";
            code.AppendLine($$"""
                    sealed class Screen{{screen}} : IScreen{{disposable}}
                    {
                        {{members}}
                        {{initializer}}
                        public string Run(int step, int steps)
                        {
                            var current = $"{{current}}";
                            if (step == steps) return current;
                            IScreen next = {{next}};
                            return current + " " + next.Run(step + 1, steps);
                        }
                        {{dispose}}
                    }
                """);

            var contract = Topology == Topology.Enumerable ? "IScreen" : $"Screen{screen}";
            var tags = Topology == Topology.Enumerable ? $"{screen}" : HasTags ? "\"a\", \"b\"" : "";
            if (IsFactory)
            {
                var title = Creation == Creation.FactoryWithOverride ? $"ctx.Override(new Title(\"T{screen}\")); " : "";
                var injections = string.Join(" ", dependencies.Select(i => $"ctx.Inject({(i.Tag is null ? "" : $"\"{i.Tag}\", ")}out {i.Type} {ToParameter(i.Name)});"));
                var arguments = string.Join(", ", dependencies.Select(i => ToParameter(i.Name)));
                bindings.AppendLine($"            .Bind<{contract}>({tags}).As(Lifetime.{Lifetime}).To(ctx => {{ {title}{injections} return new Screen{screen}({arguments}); }})");
            }
            else if (Lifetime != "Transient" || tags.Length > 0)
            {
                bindings.AppendLine($"            .Bind<{contract}>({tags}).As(Lifetime.{Lifetime}).To<Screen{screen}>()");
            }
        }

        private static string ToParameter(string name) => char.ToLowerInvariant(name[0]) + name[1..];

        // Simulates the navigation. A screen reads the argument of the delegate that creates it,
        // and the session and the title do not depend on the path to the screen.
        public string GetExpectedPath(int size)
        {
            var path = new List<string>();
            var screen = 0;
            for (var step = 0; step <= Steps; step++)
            {
                var state = new List<string>();
                if (HasArgument)
                {
                    state.Add(step == 0 ? "start" : $"c{step - 1}");
                }

                if (HasSession)
                {
                    state.Add("session");
                }

                if (Creation == Creation.FactoryWithOverride)
                {
                    state.Add($"T{screen}");
                }

                if (ServiceLifetime is not null)
                {
                    state.Add(ServiceReadsSession ? "session" : "v");
                }

                path.Add(state.Count == 0 ? $"S{screen}" : $"S{screen}({string.Join(",", state)})");
                screen = Topology == Topology.Router
                    ? (screen + 1 + step % 2) % size
                    : Targets(size, screen)[step % 2];
            }

            return string.Join(" ", path);
        }

        public override string ToString()
        {
            var name = new StringBuilder($"{Topology} {Edge} {Lifetime} {Creation} {Mode}");
            if (Topology == Topology.Router)
            {
                name.Append($" router={RouterLifetime}");
            }

            if (HasSession)
            {
                name.Append(" session");
            }

            if (ServiceLifetime is not null)
            {
                name.Append($" service={ServiceLifetime}");
            }

            if (HasTags)
            {
                name.Append(" tags");
            }

            if (Roots > 1)
            {
                name.Append($" roots={Roots}");
            }

            return name.ToString();
        }
    }
}
