namespace Pure.DI.IntegrationTests;

using System.Text;
using Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;

/// <summary>
/// Exercises paths outside the constructor/Func diamonds in ScalabilityTests. Each sample adds a
/// constant number of bindings per level, but has twice as many dependency paths at the next level.
/// Tokens and construction expressions are counted: counting lines misses nested new expressions
/// on the same line. Runtime checks prevent a smaller but incorrectly shared graph from passing.
/// </summary>
[Trait("Category", "CodeSize")]
public class GeneratedCodeGrowthTests(ITestOutputHelper output)
{
    private static readonly int[] Depths = [3, 6, 9];

    public static IEnumerable<object[]> MemberCases() =>
        from edge in new[] { Edge.Property, Edge.Field, Edge.Method, Edge.BuildUp }
        from lifetime in new[] { "Transient", "PerBlock", "PerResolve", "Scoped", "Singleton" }
        from mode in new[] { "Auto", "On" }
        select new object[] { edge, lifetime, mode };

    public static IEnumerable<object[]> CollectionCases() =>
        from edge in new[] { Edge.Array, Edge.Span, Edge.ReadOnlySpan, Edge.Memory, Edge.ReadOnlyMemory, Edge.Enumerable, Edge.AsyncEnumerable }
        from lifetime in new[] { "Transient", "PerResolve", "Singleton" }
        from mode in new[] { "Auto", "On" }
        // Stored consumers of spans are rejected by ref-safety validation (DIE046).
        // Exercise supported graphs here; diagnostic coverage belongs to BclInjectionTests.
        where edge is not (Edge.Span or Edge.ReadOnlySpan) || lifetime == "Transient"
        select new object[] { edge, lifetime, mode };

    [Theory]
    [MemberData(nameof(MemberCases))]
    public Task ShouldGrowLinearlyThroughMemberInjection(Edge edge, string lifetime, string mode) =>
        CheckGrowth(depth => CreateDiamond(depth, edge, lifetime, mode), lifetime);

    [Theory]
    [MemberData(nameof(CollectionCases))]
    public Task ShouldGrowLinearlyThroughCollections(Edge edge, string lifetime, string mode) =>
        CheckGrowth(depth => CreateDiamond(depth, edge, lifetime, mode), lifetime);

    [Theory]
    [InlineData("Transient", "Auto")]
    [InlineData("Transient", "On")]
    [InlineData("PerResolve", "Auto")]
    [InlineData("PerResolve", "On")]
    [InlineData("Singleton", "Auto")]
    [InlineData("Singleton", "On")]
    public Task ShouldGrowLinearlyWhenBuildingUpAnExistingRoot(string lifetime, string mode) =>
        CheckGrowth(depth => CreateDiamond(depth, Edge.Property, lifetime, mode, builder: true), lifetime);

    [Theory]
    [InlineData("Transient", "Auto")]
    [InlineData("Transient", "On")]
    [InlineData("PerResolve", "Auto")]
    [InlineData("PerResolve", "On")]
    [InlineData("Singleton", "Auto")]
    [InlineData("Singleton", "On")]
    public Task ShouldGrowLinearlyForLightweightRoots(string lifetime, string mode) =>
        CheckGrowth(depth => CreateDiamond(depth, Edge.Constructor, lifetime, mode, light: true), lifetime);

    [Theory]
    [InlineData(false, "Auto")]
    [InlineData(false, "On")]
    [InlineData(true, "Auto")]
    [InlineData(true, "On")]
    public async Task ShouldGrowLinearlyForGenericRootsWithArguments(bool factory, string mode)
    {
        var measurements = new List<Measurement>();
        foreach (var depth in Depths)
        {
            var result = await CreateGenericDiamond(depth, factory, mode).RunAsync(new Options(Options.ModernLanguageVersion));
            CheckGeneration(result);
            var leaves = 1 << depth;
            // Invoke each closed root twice with different arguments. Shared generated helpers
            // must preserve both the generic substitution and the argument of the current call.
            result.StdOut.ShouldBe([
                $"{leaves} {leaves} True 42", $"{leaves} {2 * leaves} True 43",
                $"{leaves} {leaves} True a", $"{leaves} {2 * leaves} True b"], result);
            measurements.Add(Measure(depth, result));
        }

        ShouldGrowLinearly(measurements);
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("On")]
    public async Task ShouldGrowLinearlyWhenFactoriesReadTheInjectionTag(string mode)
    {
        var measurements = new List<Measurement>();
        foreach (var depth in Depths)
        {
            var result = await CreateTaggedDiamond(depth, mode).RunAsync(new Options(Options.ModernLanguageVersion));
            CheckGeneration(result);
            result.StdOut.ShouldBe([$"{1 << depth} True root"], result);
            measurements.Add(Measure(depth, result));
        }

        ShouldGrowLinearly(measurements);
    }

    [Theory]
    [InlineData("PerBlock", "Auto")]
    [InlineData("PerBlock", "On")]
    [InlineData("PerResolve", "Auto")]
    [InlineData("PerResolve", "On")]
    public Task ShouldGrowLinearlyWhenTransientSiblingsShareADependency(string dependencyLifetime, string mode) =>
        // Every transient reads the same dependency. Extracting a function must keep this sharing,
        // but expanding the entire tree in place is not necessary to preserve it.
        CheckGrowth(depth => CreateDiamond(depth, Edge.Constructor, "Transient", mode, dependencyLifetime: dependencyLifetime), "Transient");

    [Theory]
    [InlineData(Boundary.Owned, "Auto")]
    [InlineData(Boundary.Owned, "On")]
    [InlineData(Boundary.Accumulator, "Auto")]
    [InlineData(Boundary.Accumulator, "On")]
    public async Task ShouldGrowLinearlyThroughNestedAccumulatorBoundaries(Boundary boundary, string mode)
    {
        var measurements = new List<Measurement>();
        foreach (var depth in Depths)
        {
            var result = await CreateBoundaries(depth, boundary, mode).RunAsync(new Options(Options.ModernLanguageVersion));
            CheckGeneration(result);
            var leaves = 1 << depth;
            // Each branch owns its leaves or collects them in its own bag. Hoisting code must
            // neither share transient leaves nor capture another invocation's owner/accumulator.
            result.StdOut.ShouldBe([$"{leaves} {leaves} True"], result);
            measurements.Add(Measure(depth, result));
        }

        ShouldGrowLinearly(measurements);
    }

    [Fact]
    public async Task ShouldDetectExpansionEvenWhenItFitsOnOneLine()
    {
        // Negative control: disabling extraction deliberately repeats a constructor tree inside
        // one return expression. A line-count-only assertion would accept this regression.
        var measurements = new List<Measurement>();
        foreach (var depth in Depths)
        {
            var result = await CreateDiamond(depth, Edge.Constructor, "Transient", "Off")
                .RunAsync(new Options(Options.ModernLanguageVersion));
            CheckGeneration(result);
            result.StdOut.ShouldBe([$"{1 << depth} {1 << depth} True"], result);
            var measurement = Measure(depth, result);
            measurement.Constructions.ShouldBe((1 << (depth + 1)) - 1, result);
            measurements.Add(measurement);
        }

        GrowthRatio(measurements.Select(i => i.Tokens).ToArray()).ShouldBeGreaterThan(4);
    }

    private async Task CheckGrowth(Func<int, string> createSource, string lifetime)
    {
        var measurements = new List<Measurement>();
        foreach (var depth in Depths)
        {
            var result = await createSource(depth).RunAsync(new Options(Options.ModernLanguageVersion));
            CheckGeneration(result);
            var leaves = 1 << depth;
            result.StdOut.ShouldBe([$"{leaves} {(lifetime == "Transient" ? leaves : 1)} True"], result);
            measurements.Add(Measure(depth, result));
        }

        ShouldGrowLinearly(measurements);
    }

    private static void CheckGeneration(Result result)
    {
        result.Errors.ShouldBeEmpty(result);
        // DIW016 is evidence of the size problem, not a reason to skip measuring it. Reject all
        // other warnings; runtime output below also detects failures hidden by Result.Success.
        result.Warnings.Where(i => i.Id != LogId.WarningRootCodeIsTooLarge).ShouldBeEmpty(result);
        result.GeneratedCode.ShouldNotBeNullOrWhiteSpace(result);
    }

    private Measurement Measure(int depth, Result result)
    {
        // Result also contains the embedded Pure.DI API. Measure only the generated composition
        // and its supporting types, so a change in the API cannot dilute the measurements.
        var namespaces = CSharpSyntaxTree.ParseText(result.GeneratedCode).GetRoot().DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>().Where(i => i.Name.ToString() == "Sample").ToArray();
        namespaces.ShouldNotBeEmpty(result);
        var constructions = namespaces.SelectMany(i => i.DescendantNodes()).OfType<ObjectCreationExpressionSyntax>()
            .Count(i => IsGraphType(i.Type));
        constructions.ShouldBeGreaterThan(0, result);
        var measurement = new Measurement(depth, namespaces.Sum(i => i.DescendantTokens().Count()), constructions,
            result.Warnings.Count(i => i.Id == LogId.WarningRootCodeIsTooLarge));
        output.WriteLine(measurement.ToString());
        return measurement;
    }

    private static bool IsGraphType(TypeSyntax type)
    {
        var name = type switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => ""
        };
        return name.Length > 1 && name[0] == 'N' && name.Skip(1).All(char.IsDigit);
    }

    private static void ShouldGrowLinearly(IReadOnlyList<Measurement> measurements)
    {
        var details = string.Join(Environment.NewLine, measurements);
        // Equal steps in graph size must add approximately equal amounts of code. A 35% margin
        // permits extraction thresholds and exposes superlinear growth at these depths.
        GrowthRatio(measurements.Select(i => i.Tokens).ToArray()).ShouldBeLessThan(1.35, details);
        GrowthRatio(measurements.Select(i => i.Constructions).ToArray()).ShouldBeLessThan(1.35, details);
        measurements.Select(i => i.SizeWarnings).ShouldAllBe(i => i == 0, details);
    }

    private static double GrowthRatio(IReadOnlyList<int> sizes)
    {
        var firstStep = sizes[1] - sizes[0];
        var lastStep = sizes[2] - sizes[1];
        // Constant size is also bounded. A zero first step followed by growth is not linear.
        return firstStep <= 0 ? lastStep <= 0 ? 1 : double.PositiveInfinity : (double)lastStep / firstStep;
    }

    private static string CreateDiamond(int depth, Edge edge, string lifetime, string mode, bool builder = false, string? dependencyLifetime = null, bool light = false)
    {
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        var hasDependency = dependencyLifetime is not null;
        var dependencyParameter = hasDependency ? "Token token, " : "";
        var dependencyCheck = hasDependency ? "ReferenceEquals(Token, Left.Token) && ReferenceEquals(Token, Right.Token) && " : "";
        var same = lifetime != "Transient";
        classes.AppendLine($$"""
            public sealed class Token { }
            public sealed class N0
            {
                public static int Created;
                public N0({{(hasDependency ? "Token token" : "")}}) { Created++; {{(hasDependency ? "Token = token;" : "")}} }
                public int Leaves => 1;
                public bool Valid => true;
                {{(hasDependency ? "public Token Token { get; }" : "")}}
            }
            """);
        bindings.AppendLine($".Bind<N0>().As(Lifetime.{lifetime}).To<N0>()");
        if (hasDependency)
        {
            bindings.AppendLine($".Bind<Token>().As(Lifetime.{dependencyLifetime}).To<Token>()");
        }

        for (var level = 1; level <= depth; level++)
        {
            var previous = $"N{level - 1}";
            var parameters = $"{dependencyParameter}{previous} left, {previous} right";
            var assign = $"Left = left; Right = right;{(hasDependency ? " Token = token;" : "")}";
            var members = $"public {previous} Left {{ get; private set; }} = null!; public {previous} Right {{ get; private set; }} = null!;";
            var constructor = $"public N{level}({parameters}) {{ {assign} }}";
            switch (edge)
            {
                case Edge.Property:
                case Edge.BuildUp:
                    constructor = "";
                    members = $"[Ordinal(0)] public {previous} Left {{ get; set; }} = null!; [Ordinal(1)] public {previous} Right {{ get; set; }} = null!;";
                    break;
                case Edge.Field:
                    constructor = "";
                    members = $"[Ordinal(0)] public {previous} Left = null!; [Ordinal(1)] public {previous} Right = null!;";
                    break;
                case Edge.Method:
                    constructor = $"[Ordinal(0)] public void Initialize({parameters}) {{ {assign} }}";
                    break;
                case Edge.Array:
                case Edge.Span:
                case Edge.ReadOnlySpan:
                case Edge.Memory:
                case Edge.ReadOnlyMemory:
                case Edge.Enumerable:
                case Edge.AsyncEnumerable:
                    var collection = edge switch
                    {
                        Edge.Array => $"{previous}[]",
                        Edge.Enumerable => $"IEnumerable<{previous}>",
                        Edge.Span or Edge.ReadOnlySpan or Edge.Memory or Edge.ReadOnlyMemory => $"{edge}<{previous}>",
                        _ => $"IAsyncEnumerable<{previous}>"
                    };
                    var access = edge switch
                    {
                        Edge.AsyncEnumerable => "Program.Single({0})",
                        Edge.Span or Edge.ReadOnlySpan => "{0}[0]",
                        Edge.Memory or Edge.ReadOnlyMemory => "{0}.Span[0]",
                        _ => "{0}.Single()"
                    };
                    var first = string.Format(access, "left");
                    var second = string.Format(access, "right");
                    // Span injection into a heap object's constructor intentionally produces
                    // DIW012/DIE046. Method injection consumes the span without storing it.
                    constructor = edge is Edge.Span or Edge.ReadOnlySpan
                        ? $"[Ordinal(0)] public void Initialize({collection} left, {collection} right) {{ Left = {first}; Right = {second}; }}"
                        : $"public N{level}({collection} left, {collection} right) {{ Left = {first}; Right = {second}; }}";
                    break;
            }

            classes.AppendLine($$"""
                public sealed class N{{level}}
                {
                    {{constructor}}
                    {{members}}
                    {{(hasDependency ? "public Token Token { get; }" : "")}}
                    public int Leaves => Left.Leaves + Right.Leaves;
                    public bool Valid => {{dependencyCheck}}ReferenceEquals(Left, Right) == {{same.ToString().ToLowerInvariant()}} && Left.Valid && Right.Valid;
                }
                """);
            // A builder receives the top object from the caller; do not bind that top object.
            if (!builder || level < depth)
            {
                bindings.Append($".Bind<N{level}>().As(Lifetime.{lifetime})");
                bindings.AppendLine(edge == Edge.BuildUp
                    ? $".To(ctx => {{ var value = new N{level}(); ctx.BuildUp(value); return value; }})"
                    : $".To<N{level}>()");
            }
        }

        var root = builder ? $".Builder<N{depth}>(\"Build\")" : $".Root<N{depth}>(\"Root\"{(light ? ", kind: RootKinds.Light" : "")})";
        var resolve = builder ? $"composition.Build(new N{depth}())" : "composition.Root";
        return $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;
            using Pure.DI;
            namespace Sample
            {
                {{classes}}
                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Hint(Hint.LocalFunctions, "{{mode}}")
                        {{bindings}}
                        {{root}};
                }
                public static class Program
                {
                    public static T Single<T>(IAsyncEnumerable<T> values)
                    {
                        var iterator = values.GetAsyncEnumerator();
                        try
                        {
                            if (!iterator.MoveNextAsync().AsTask().GetAwaiter().GetResult()) throw new Exception("Empty");
                            var value = iterator.Current;
                            if (iterator.MoveNextAsync().AsTask().GetAwaiter().GetResult()) throw new Exception("Multiple");
                            return value;
                        }
                        finally { iterator.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
                    }
                    public static void Main()
                    {
                        var composition = new Composition();
                        var root = {{resolve}};
                        Console.WriteLine($"{root.Leaves} {N0.Created} {root.Valid}");
                    }
                }
            }
            """;
    }

    private static string CreateTaggedDiamond(int depth, string mode)
    {
        var classes = new StringBuilder("""
            sealed class N0
            {
                public static int Created;
                public N0(string tag) { Created++; Tag = tag; }
                public string Tag { get; }
                public bool Valid => true;
            }
            """);
        var bindings = new StringBuilder(".Bind<N0>(Tag.Any).To(ctx => new N0(ctx.Tag?.ToString() ?? \"\"))");
        for (var level = 1; level <= depth; level++)
        {
            classes.AppendLine($$"""
                sealed class N{{level}}
                {
                    public N{{level}}(string tag, N{{level - 1}} left, N{{level - 1}} right)
                    {
                        Tag = tag;
                        Valid = left.Tag == "a" && right.Tag == "b" && !ReferenceEquals(left, right) && left.Valid && right.Valid;
                    }
                    public string Tag { get; }
                    public bool Valid { get; }
                }
                """);
            bindings.AppendLine($".Bind<N{level}>(Tag.Any).To(ctx => {{ ctx.Inject(\"a\", out N{level - 1} left); ctx.Inject(\"b\", out N{level - 1} right); return new N{level}(ctx.Tag?.ToString() ?? \"\", left, right); }})");
        }

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
                        .Root<N{{depth}}>("Root", "root");
                }
                public static class Program
                {
                    public static void Main()
                    {
                        var root = new Composition().Root;
                        Console.WriteLine($"{N0.Created} {root.Valid} {root.Tag}");
                    }
                }
            }
            """;
    }

    private static string CreateGenericDiamond(int depth, bool factory, string mode)
    {
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        classes.AppendLine("""
            sealed class N0<T>
            {
                public static int Created;
                public N0(T value) { Created++; Value = value; }
                public T Value { get; }
                public int Leaves => 1;
                public bool Valid => true;
            }
            """);
        bindings.AppendLine(factory
            ? ".Bind<N0<TT>>().To(ctx => { ctx.Inject(out TT value); return new N0<TT>(value); })"
            : ".Bind<N0<TT>>().To<N0<TT>>()");
        for (var level = 1; level <= depth; level++)
        {
            classes.AppendLine($$"""
                sealed class N{{level}}<T>
                {
                    public N{{level}}(N{{level - 1}}<T> left, N{{level - 1}}<T> right)
                    {
                        Value = left.Value;
                        Leaves = left.Leaves + right.Leaves;
                        Valid = !ReferenceEquals(left, right) && left.Valid && right.Valid && EqualityComparer<T>.Default.Equals(left.Value, right.Value);
                    }
                    public T Value { get; }
                    public int Leaves { get; }
                    public bool Valid { get; }
                }
                """);
            bindings.AppendLine(factory
                ? $".Bind<N{level}<TT>>().To(ctx => {{ ctx.Inject(out N{level - 1}<TT> left); ctx.Inject(out N{level - 1}<TT> right); return new N{level}<TT>(left, right); }})"
                : $".Bind<N{level}<TT>>().To<N{level}<TT>>()");
        }

        return $$"""
            using System;
            using System.Collections.Generic;
            using Pure.DI;
            namespace Sample
            {
                {{classes}}
                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Hint(Hint.LocalFunctions, "{{mode}}")
                        .Hint(Hint.Resolve, "Off")
                        .RootArg<TT>("value")
                        {{bindings}}
                        .Root<N{{depth}}<TT>>("Get");
                }
                public static class Program
                {
                    private static void Run<T>(Composition composition, T value)
                    {
                        var root = composition.Get<T>(value);
                        Console.WriteLine($"{root.Leaves} {N0<T>.Created} {root.Valid} {root.Value}");
                    }
                    public static void Main()
                    {
                        var composition = new Composition();
                        Run(composition, 42); Run(composition, 43);
                        Run(composition, "a"); Run(composition, "b");
                    }
                }
            }
            """;
    }

    private static string CreateBoundaries(int depth, Boundary boundary, string mode)
    {
        var owned = boundary == Boundary.Owned;
        var classes = new StringBuilder();
        for (var level = 1; level <= depth; level++)
        {
            var item = owned ? $"Owned<N{level - 1}>" : $"Collected<N{level - 1}>";
            var capture = owned ? "using var l = left(); using var r = right();" : "var l = left(); var r = right();";
            var checkBag = owned ? "" : $" && l.Count == {(level == 1 ? 1 : 0)} && r.Count == {(level == 1 ? 1 : 0)} && !ReferenceEquals(l.Owner, r.Owner)";
            classes.AppendLine($$"""
                sealed class N{{level}}
                {
                    public N{{level}}(Func<{{item}}> left, Func<{{item}}> right)
                    {
                        {{capture}}
                        Leaves = l.Value.Leaves + r.Value.Leaves;
                        Valid = l.Value.Valid && r.Value.Valid && !ReferenceEquals(l.Value, r.Value){{checkBag}};
                        {{(owned ? "" : "N0.Collected += l.Count + r.Count;")}}
                    }
                    public int Leaves { get; }
                    public bool Valid { get; }
                }
                """);
        }

        return $$"""
            using System;
            using System.Collections.Generic;
            using Pure.DI;
            namespace Sample
            {
                interface IBag { int Count { get; } }
                sealed class Bag : List<N0>, IBag { }
                sealed class Collected<T> : IBag
                {
                    public Collected(T value, IBag owner) { Value = value; Owner = owner; }
                    public T Value { get; }
                    public IBag Owner { get; }
                    public int Count => Owner.Count;
                }
                public sealed class N0 : IDisposable
                {
                    public static int Created;
                    public static int Disposed;
                    public static int Collected;
                    public N0() { Created++; }
                    public void Dispose() { Disposed++; }
                    public int Leaves => 1;
                    public bool Valid => true;
                }
                {{classes}}
                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Hint(Hint.LocalFunctions, "{{mode}}")
                        {{(owned ? "" : """
                        .Accumulate<N0, Bag>(Lifetime.Transient)
                        .Bind<IBag>().As(Lifetime.PerBlock).To(ctx => { ctx.Inject(out Bag bag); return bag; })
                        .Bind<Collected<TT>>().To<Collected<TT>>()
                        """)}}
                        .Root<N{{depth}}>("Root");
                }
                public static class Program
                {
                    public static void Main()
                    {
                        var root = new Composition().Root;
                        Console.WriteLine($"{N0.Created} { {{(owned ? "N0.Disposed" : "N0.Collected")}} } {root.Valid && root.Leaves == {{1 << depth}}}");
                    }
                }
            }
            """;
    }

    [Fact]
    public async Task ShouldKeepCustomAccumulatorVariablesInScopeWithoutLocalFunctions()
    {
        // Check the custom accumulator sample independently of extraction and generated size:
        // a failure here distinguishes accumulator scope handling from helper hoisting.
        var result = await CreateBoundaries(3, Boundary.Accumulator, "Off")
            .RunAsync(new Options(Options.ModernLanguageVersion));
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["8 8 True"], result);
    }

    private sealed record Measurement(int Depth, int Tokens, int Constructions, int SizeWarnings);

    public enum Edge { Constructor, Property, Field, Method, BuildUp, Array, Span, ReadOnlySpan, Memory, ReadOnlyMemory, Enumerable, AsyncEnumerable }

    public enum Boundary { Owned, Accumulator }
}
