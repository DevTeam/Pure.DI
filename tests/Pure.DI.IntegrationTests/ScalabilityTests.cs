using System.Text;

namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
/// The size of the generated code grows linearly with the size of the graph, see #172, #175, #177 and #178.
/// A graph is generated in three sizes. The code that does not depend on the size is the same in all of them,
/// so for a linear growth the code added by the last step is about twice as large as the code added by the first one,
/// while for a growth with the number of dependency paths it is many times larger.
/// </summary>
public class ScalabilityTests
{
    private static readonly int[] Sizes = [4, 6, 10];
    // The size grows by 2 and then by 4, so a linear growth adds about twice as much code in the last step.
    private const double MaxGrowthRatio = 3;

    public static IEnumerable<object[]> DiamondCases() =>
        from lifetime in new[] { "Transient", "PerBlock", "PerResolve", "Singleton" }
        from creation in new[] { Creation.Constructor, Creation.Factory }
        from context in new[] { DiamondContext.Root, DiamondContext.Lazy, DiamondContext.FuncArgument, DiamondContext.FuncArgumentWithBackReference }
        select new object[] { lifetime, creation, context };

    public static IEnumerable<object[]> NestedFuncCases() =>
        from creation in new[] { Creation.Constructor, Creation.Factory }
        from arguments in new[] { NestedFuncArguments.ConsumedByLast, NestedFuncArguments.ConsumedByEach, NestedFuncArguments.OnePerFunc }
        select new object[] { creation, arguments };

    [Theory]
    [MemberData(nameof(DiamondCases))]
    public async Task ShouldGrowLinearlyForDiamond(string lifetime, Creation creation, DiamondContext context)
    {
        // Given

        // When
        var lines = new List<int>();
        foreach (var size in Sizes)
        {
            var result = await CreateDiamond(size, lifetime, creation, context).RunAsync(new Options(Options.ModernLanguageVersion));

            // Then
            // A stored lifetime built from a delegate argument is reported as a warning.
            result.Errors.Count.ShouldBe(0, result);
            result.StdOut.ShouldBe([$"S{size - 1}"], result);
            lines.Add(CountGraphLines(result));
        }

        ShouldGrowLinearly(lines);
    }

    [Theory]
    [MemberData(nameof(NestedFuncCases))]
    public async Task ShouldGrowLinearlyForNestedFuncs(Creation creation, NestedFuncArguments arguments)
    {
        // Given

        // When
        var lines = new List<int>();
        foreach (var size in Sizes)
        {
            var result = await CreateNestedFuncs(size, creation, arguments).RunAsync(new Options(Options.ModernLanguageVersion));

            // Then
            result.Success.ShouldBeTrue(result);
            result.StdOut.ShouldBe([$"Vm{size}"], result);
            lines.Add(CountGraphLines(result));
        }

        ShouldGrowLinearly(lines);
    }

#if ROSLYN4_8_OR_GREATER
    // Each level reaches the next one by two dependency paths, so the number of paths to the last level grows as the powers of 2.
    // The code that creates an instance must not be repeated on each of these paths, see #186.
    public static IEnumerable<object[]> BranchingCases() =>
        from lifetime in new[] { "Transient", "PerResolve", "Scoped", "Singleton" }
        from edges in new[]
        {
            (Edge.FuncArgumentConsumed, Edge.FuncArgumentConsumed),
            (Edge.FuncArgumentConsumed, Edge.FuncArgumentNotConsumed),
            (Edge.FuncArgumentNotConsumed, Edge.FuncArgumentNotConsumed),
            (Edge.FuncArgumentConsumed, Edge.Func),
            (Edge.FuncArgumentConsumed, Edge.Lazy),
            (Edge.FuncArgumentConsumed, Edge.Direct),
            (Edge.Func, Edge.Lazy)
        }
        from funcBinding in new[] { FuncBinding.BuiltIn, FuncBinding.Let }
        from localFunctions in new[] { "On", "Auto" }
        // A delegate without arguments does not depend on the binding of Func<T1, T>.
        where funcBinding == FuncBinding.BuiltIn || edges.Item1 is Edge.FuncArgumentConsumed or Edge.FuncArgumentNotConsumed
        select new object[] { lifetime, edges.Item1, edges.Item2, funcBinding, localFunctions };

    [Theory]
    [MemberData(nameof(BranchingCases))]
    public async Task ShouldGrowLinearlyForBranching(string lifetime, Edge first, Edge second, FuncBinding funcBinding, string localFunctions)
    {
        // Given
        var isShared = lifetime is "Scoped" or "Singleton";

        // When
        var lines = new List<int>();
        foreach (var size in Sizes)
        {
            var result = await CreateBranching(size, lifetime, first, second, funcBinding, localFunctions, isShared).RunAsync(new Options(Options.ModernLanguageVersion));

            // Then
            result.Errors.Count.ShouldBe(0, result);
            result.StdOut.ShouldBe([string.Concat(Enumerable.Range(0, size).Select(i => $"S{i}"))], result);
            lines.Add(CountGraphLines(result));
            if (isShared && localFunctions == "On")
            {
                // A shared instance that does not read the arguments of the delegates it is reached from is created in one place.
                var constructions = result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains($"new global::Sample.S{size - 1}("));
                constructions.ShouldBe(1, $"Constructions of S{size - 1} of {size} levels");
            }
        }

        ShouldGrowLinearly(lines);
    }
#endif

    [Fact]
    public async Task ShouldWarnWhenCodeOfRootIsTooLarge()
    {
        // Given

        // When
        // Without local functions a transient instance is created in place on each dependency path.
        var result = await CreateDiamond(16, "Transient", Creation.Constructor, DiamondContext.FuncArgumentWithBackReference, "Off")
            .RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Select(i => i.Id).ShouldBe([LogId.WarningRootCodeIsTooLarge], result);
        // The arguments of delegates that reach nested delegates can make the code large too, see #184.
        result.Warnings.Single().Message.ShouldContain("ctx.Let");
        result.StdOut.ShouldBe(["S15"], result);
    }

    // The code of the API does not depend on the graph, so only the lines that refer to the types of the sample are counted.
    private static int CountGraphLines(Result result) =>
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("global::Sample."));

    private static void ShouldGrowLinearly(List<int> lines)
    {
        var firstStep = Math.Max(lines[1] - lines[0], 1);
        var lastStep = lines[2] - lines[1];
        ((double)lastStep / firstStep).ShouldBeLessThan(MaxGrowthRatio, $"Generated lines for sizes {string.Join(", ", Sizes)}: {string.Join(", ", lines)}");
    }

    // S{i} depends on S{i-1} and S{i-2}, so the number of dependency paths to S0 grows as the Fibonacci numbers.
    private static string CreateDiamond(int size, string lifetime, Creation creation, DiamondContext context, string? localFunctions = null)
    {
        var top = $"S{size - 1}";
        var s0Parameters = context switch
        {
            DiamondContext.FuncArgument => "string label",
            DiamondContext.FuncArgumentWithBackReference => $"string label, Lazy<{top}> top",
            _ => ""
        };

        var classes = new StringBuilder();
        classes.AppendLine($"public sealed class S0 {{ public S0({s0Parameters}) {{ }} }}");
        classes.AppendLine("public sealed class S1 { public S1(S0 a) { } }");
        for (var i = 2; i < size; i++)
        {
            classes.AppendLine($"public sealed class S{i} {{ public S{i}(S{i - 1} a, S{i - 2} b) {{ }} }}");
        }

        var bindings = new StringBuilder();
        for (var i = 0; i < size; i++)
        {
            bindings.Append($".Bind<S{i}>().As(Lifetime.{lifetime})");
            if (creation == Creation.Constructor)
            {
                bindings.AppendLine($".To<S{i}>()");
                continue;
            }

            var injections = i switch
            {
                0 => context switch
                {
                    DiamondContext.FuncArgument => ("ctx.Inject(out string label);", "label"),
                    DiamondContext.FuncArgumentWithBackReference => ($"ctx.Inject(out string label); ctx.Inject(out Lazy<{top}> top);", "label, top"),
                    _ => ("", "")
                },
                1 => ("ctx.Inject(out S0 a);", "a"),
                _ => ($"ctx.Inject(out S{i - 1} a); ctx.Inject(out S{i - 2} b);", "a, b")
            };

            bindings.AppendLine($".To(ctx => {{ {injections.Item1} return new S{i}({injections.Item2}); }})");
        }

        var (rootType, rootValue) = context switch
        {
            DiamondContext.Lazy => ($"Lazy<{top}>", "Root.Value"),
            DiamondContext.FuncArgument or DiamondContext.FuncArgumentWithBackReference => ($"Func<string, {top}>", "Root(\"x\")"),
            _ => (top, "Root")
        };

        return $$"""
                 using System;
                 using Pure.DI;

                 namespace Sample
                 {
                     {{classes}}

                     partial class Composition
                     {
                         private static void Setup() =>
                             DI.Setup(nameof(Composition))
                                 {{(localFunctions is null ? "" : $".Hint(Hint.LocalFunctions, \"{localFunctions}\")")}}
                                 {{bindings}}
                                 .Root<{{rootType}}>("Root");
                     }

                     public static class Program
                     {
                         public static void Main() => Console.WriteLine(new Composition().{{rootValue}}.GetType().Name);
                     }
                 }
                 """;
    }

    // Each view model opens the next one through two delegates, so the number of chains of delegates grows as the powers of 2.
    private static string CreateNestedFuncs(int size, Creation creation, NestedFuncArguments arguments)
    {
        var (parameters, lastParameters, openWithB, callWithB) = arguments switch
        {
            NestedFuncArguments.ConsumedByLast => ("", "Nav nav", "Func<ArgB, Vm{0}>", "OpenWithB(new ArgB())"),
            NestedFuncArguments.ConsumedByEach => ("Nav nav, ", "Nav nav", "Func<Nav, ArgB, Vm{0}>", "OpenWithB(new Nav(), new ArgB())"),
            _ => ("Nav nav, ArgB b, ", "Nav nav, ArgB b", "Func<ArgB, Vm{0}>", "OpenWithB(new ArgB())")
        };

        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        for (var i = 0; i < size; i++)
        {
            var openWithBType = string.Format(openWithB, i + 1);
            classes.AppendLine(
                $$"""
                  public sealed class Vm{{i}} : IVm
                  {
                      public Vm{{i}}({{parameters}}Service service, Func<Nav, Vm{{i + 1}}> open, {{openWithBType}} openWithB)
                      {
                          Open = open;
                          OpenWithB = openWithB;
                      }

                      public Func<Nav, Vm{{i + 1}}> Open { get; }

                      public {{openWithBType}} OpenWithB { get; }

                      public IVm Next(int index) => index % 2 == 0 ? {{callWithB}} : Open(new Nav());
                  }
                  """);

            if (creation == Creation.Factory)
            {
                var names = parameters.Split(", ", StringSplitOptions.RemoveEmptyEntries).Select(i => i.Split(' ')).ToList();
                var injections = string.Concat(names.Select(i => $"ctx.Inject(out {i[0]} {i[1]}); "));
                var arguments2 = string.Concat(names.Select(i => $"{i[1]}, "));
                bindings.AppendLine($".Bind<Vm{i}>().To(ctx => {{ {injections}ctx.Inject(out Service service); ctx.Inject(out Func<Nav, Vm{i + 1}> open); ctx.Inject(out {openWithBType} openWithB); return new Vm{i}({arguments2}service, open, openWithB); }})");
            }
        }

        if (creation == Creation.Factory)
        {
            var names = lastParameters.Split(", ").Select(i => i.Split(' ')).ToList();
            var injections = string.Concat(names.Select(i => $"ctx.Inject(out {i[0]} {i[1]}); "));
            bindings.AppendLine($".Bind<Vm{size}>().To(ctx => {{ {injections}return new Vm{size}({string.Join(", ", names.Select(i => i[1]))}); }})");
        }

        return $$"""
                 using System;
                 using Pure.DI;

                 namespace Sample
                 {
                     public interface IVm
                     {
                         IVm Next(int index);
                     }

                     public sealed class Nav { }

                     public sealed class ArgB { }

                     public sealed class Service { }

                     {{classes}}

                     public sealed class Vm{{size}} : IVm
                     {
                         public Vm{{size}}({{lastParameters}}) { }

                         public IVm Next(int index) => this;

                         public override string ToString() => GetType().Name;
                     }

                     public sealed class Top
                     {
                         public Top(Func<Nav, ArgB, Vm0> open) => Open = open;

                         public Func<Nav, ArgB, Vm0> Open { get; }
                     }

                     partial class Composition
                     {
                         private static void Setup() =>
                             DI.Setup(nameof(Composition))
                                 .Bind<Service>().As(Lifetime.Singleton).To<Service>()
                                 {{bindings}}
                                 .Root<Top>("Root");
                     }

                     public static class Program
                     {
                         public static void Main()
                         {
                             IVm vm = new Composition().Root.Open(new Nav(), new ArgB());
                             for (var index = 1; index <= {{size}}; index++)
                             {
                                 vm = vm.Next(index);
                             }

                             Console.WriteLine(vm);
                         }
                     }
                 }
                 """;
    }

#if ROSLYN4_8_OR_GREATER
    // S{i} reaches S{i+1} through the items of two edges. The edges are distinguished by the types of their arguments,
    // so two delegates with arguments override different values.
    private static string CreateBranching(int size, string lifetime, Edge first, Edge second, FuncBinding funcBinding, string localFunctions, bool isShared)
    {
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        var edges = new[] { first, second };
        var argumentTypes = new[] { "A", "B" };
        for (var i = 0; i < size; i++)
        {
            var isLast = i == size - 1;
            var parameters = new List<string>();
            var items = new List<string>();
            for (var k = 0; k < edges.Length; k++)
            {
                var edge = edges[k];
                var argumentType = argumentTypes[k];
                var item = $"I{k}x{i}";
                parameters.Add(edge switch
                {
                    Edge.FuncArgumentConsumed or Edge.FuncArgumentNotConsumed => $"Func<{argumentType}, {item}> e{k}",
                    Edge.Func => $"Func<{item}> e{k}",
                    Edge.Lazy => $"Lazy<{item}> e{k}",
                    _ => $"{item} e{k}"
                });

                items.Add(edge switch
                {
                    Edge.FuncArgumentConsumed or Edge.FuncArgumentNotConsumed => $"e{k}(new {argumentType}())",
                    Edge.Func => $"e{k}()",
                    Edge.Lazy => $"e{k}.Value",
                    _ => $"e{k}"
                });

                var itemParameters = new List<string>();
                if (edge == Edge.FuncArgumentConsumed)
                {
                    itemParameters.Add($"{argumentType} argument");
                }

                if (!isLast)
                {
                    itemParameters.Add($"S{i + 1} next");
                }

                var argumentProperty = edge == Edge.FuncArgumentConsumed ? $"public {argumentType} Argument => argument;" : "";
                var nextProperty = isLast ? "public object? Next => null;" : $"public S{i + 1} Next => next;";
                classes.AppendLine(
                    $$"""
                      public sealed class {{item}}({{string.Join(", ", itemParameters)}})
                      {
                          {{argumentProperty}}
                          {{nextProperty}}
                      }
                      """);
            }

            // The items of both edges must get the same shared instance of the next level.
            var walk = isLast
                ? $"\"S{i}\""
                : isShared
                    ? $"\"S{i}\" + (ReferenceEquals(item0.Next, item1.Next) ? \"\" : \"!\") + item0.Next.Walk()"
                    : $"\"S{i}\" + item0.Next.Walk()";
            classes.AppendLine(
                $$"""
                  public sealed class S{{i}}({{string.Join(", ", parameters)}})
                  {
                      public string Walk()
                      {
                          var item0 = {{items[0]}};
                          var item1 = {{items[1]}};
                          return {{walk}};
                      }
                  }
                  """);
            bindings.AppendLine($".Bind<S{i}>().As(Lifetime.{lifetime}).To<S{i}>()");
        }

        // The argument of a delegate reaches only the item it creates.
        var funcBindingCode = funcBinding == FuncBinding.Let
            ? """
              .Bind<Func<TT1, TT>>().As(Lifetime.PerBlock).To(ctx => new Func<TT1, TT>(arg1 =>
              {
                  ctx.Let<TT1>(arg1);
                  ctx.Inject<TT>(ctx.Tag, out var value);
                  return value;
              }))
              """
            : "";

        return $$"""
                 using System;
                 using Pure.DI;

                 namespace Sample
                 {
                     public sealed class A;

                     public sealed class B;

                     {{classes}}

                     partial class Composition
                     {
                         private static void Setup() =>
                             DI.Setup(nameof(Composition))
                                 .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                                 {{funcBindingCode}}
                                 {{bindings}}
                                 .Root<S0>("Root");
                     }

                     public static class Program
                     {
                         public static void Main() => Console.WriteLine(new Composition().Root.Walk());
                     }
                 }
                 """;
    }

    public enum Edge
    {
        // A delegate with an argument that the item it creates consumes.
        FuncArgumentConsumed,
        // A delegate with an argument that the item it creates does not consume.
        FuncArgumentNotConsumed,
        Func,
        Lazy,
        Direct
    }

    public enum FuncBinding
    {
        // The built-in binding of Func<T1, T> overrides its argument.
        BuiltIn,
        // A binding of Func<T1, T> that passes its argument by ctx.Let.
        Let
    }
#endif

    public enum Creation
    {
        Constructor,
        Factory
    }

    public enum DiamondContext
    {
        Root,
        Lazy,
        FuncArgument,
        FuncArgumentWithBackReference
    }

    public enum NestedFuncArguments
    {
        // Only the last view model consumes the argument of a delegate.
        ConsumedByLast,
        // Each view model consumes the argument that both delegates pass.
        ConsumedByEach,
        // Each view model consumes two arguments, and each delegate passes one of them.
        OnePerFunc
    }
}
