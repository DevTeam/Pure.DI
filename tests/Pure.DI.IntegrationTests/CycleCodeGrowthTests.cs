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
    // The real application of #192 overrides 27 types in the scope of a screen.
    private static readonly int[] WideSizes = [9, 18, 27];
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

    // The service is created once in a block of code, so the header and the body of a screen share it.
    // The lambda that opens the next screen overrides the navigation, so its code creates the service anew
    // rather than passing the service of the screen it is created in to the next screen.
    [Theory]
    [MemberData(nameof(ArgumentCases))]
    public Task ShouldGrowLinearlyForScreensWithOverrideReadingPerBlockDiamond(CycleEdge edge, string mode) =>
        CheckGrowth(Sizes, size => new Ring(size, edge, "Transient", mode) { HasDiamond = true, ServicesLifetime = "PerBlock" });

    // Each screen also reads the contexts of the previous screens of the ring. The argument of a delegate stays
    // in the scope of the screens below it, so a screen is reached in a different override scope on each path,
    // and it reads the value of the nearest screen above it on the path, which can differ on each visit.
    // This is the shape of the real application of #192, where the override rewrite did not complete.
    [Theory]
    [MemberData(nameof(PreviousCases))]
    public Task ShouldGrowLinearlyForScreensReadingContextsOfPreviousScreens(CycleEdge edge, int previous, string mode) =>
        CheckGrowth(Sizes, size => new Ring(size, edge, "Transient", mode) { Previous = previous });

    // A ring of as many screens as the overridden types of the real application of #192,
    // whose override rewrite did not complete in time.
    [Theory]
    [InlineData(CycleEdge.FuncWithArguments)]
    [InlineData(CycleEdge.FuncWithArgumentsByLet)]
    public Task ShouldGrowLinearlyForWideScopesOfScreensReadingContextsOfPreviousScreens(CycleEdge edge) =>
        CheckGrowth(WideSizes, size => new Ring(size, edge, "Transient", "Auto") { Previous = 3 });

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

    // Screens that open each other through delegates with arguments are each created in one place,
    // so the code grows linearly with the number of screens, not with the number of navigation paths, see #192.
    private static readonly int[] RingOfDelegatesSizes = [4, 8, 12];

    [Theory]
    [InlineData(false, "Auto")]
    [InlineData(false, "On")]
    [InlineData(true, "Auto")]
    public async Task ShouldCreateEachScreenOnCycleThroughDelegatesOnce(bool letFunc, string localFunctions)
    {
        // Given

        foreach (var size in RingOfDelegatesSizes)
        {
            // When
            var result = await CreateRingOfDelegates(size, letFunc, localFunctions).RunAsync(new Options(Options.ModernLanguageVersion));

            // Then
            result.Success.ShouldBeTrue(result);
            result.StdOut.ShouldBe([GetExpectedRingOfDelegatesPath(size)], result);
            var lines = result.GeneratedCode.Split(Environment.NewLine);
            for (var screen = 0; screen < size; screen++)
            {
                lines.Count(i => i.Contains($"new global::Sample.Screen{screen}(")).ShouldBe(1, $"Screen{screen} of {size} screens{Environment.NewLine}{result.GeneratedCode}");
            }
        }
    }

    // Each screen opens the next two, so the path visits the screens by steps of one and two.
    private static string GetExpectedRingOfDelegatesPath(int size)
    {
        var path = new StringBuilder();
        var screen = 0;
        var context = "start";
        for (var step = 0; step < RingOfDelegatesSteps; step++)
        {
            path.Append($"S{screen}({context},N{step}) ");
            var next = (screen + 1 + step % 2) % size;
            context = $"c{step}";
            screen = next;
        }

        path.Append($"S{screen}({context},N{RingOfDelegatesSteps})");
        return path.ToString();
    }

    private const int RingOfDelegatesSteps = 7;

    private static string CreateRingOfDelegates(int size, bool letFunc, string localFunctions)
    {
        var classes = new StringBuilder();
        for (var i = 0; i < size; i++)
        {
            var a = (i + 1) % size;
            var b = (i + 2) % size;
            classes.AppendLine(
                $$"""
                      public sealed class Context{{i}}
                      {
                          public Context{{i}}(string value) => Value = value;

                          public string Value { get; }
                      }

                      public sealed class Screen{{i}} : IScreen
                      {
                          private readonly Context{{i}} _context;
                          private readonly INavigation _navigation;
                          private readonly Func<INavigation, Context{{a}}, Screen{{a}}> _openA;
                          private readonly Func<INavigation, Context{{b}}, Screen{{b}}> _openB;

                          public Screen{{i}}(
                              Context{{i}} context,
                              INavigation navigation,
                              Func<INavigation, Context{{a}}, Screen{{a}}> openA,
                              Func<INavigation, Context{{b}}, Screen{{b}}> openB)
                          {
                              _context = context;
                              _navigation = navigation;
                              _openA = openA;
                              _openB = openB;
                          }

                          public string Run(int step, int steps)
                          {
                              var current = $"S{{i}}({_context.Value},{_navigation})";
                              if (step == steps)
                              {
                                  return current;
                              }

                              var navigation = new Navigation(step + 1);
                              IScreen next = step % 2 == 0
                                  ? _openA(navigation, new Context{{a}}($"c{step}"))
                                  : _openB(navigation, new Context{{b}}($"c{step}"));
                              return current + " " + next.Run(step + 1, steps);
                          }
                      }

                  """);
        }

        var funcBinding = letFunc
            ? """
                          .Bind<Func<TT1, TT2, TT>>()
                              .As(Lifetime.PerBlock)
                              .To(ctx => new Func<TT1, TT2, TT>((TT1 arg1, TT2 arg2) =>
                              {
                                  ctx.Let<TT1>(arg1);
                                  ctx.Let<TT2>(arg2);
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
                     public interface INavigation { }

                     public sealed class Navigation : INavigation
                     {
                         private readonly int _id;

                         public Navigation(int id) => _id = id;

                         public override string ToString() => $"N{_id}";
                     }

                     public interface IScreen
                     {
                         string Run(int step, int steps);
                     }

                 {{classes}}
                     partial class Composition
                     {
                         private static void Setup() =>
                             DI.Setup(nameof(Composition))
                                 .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                 {{funcBinding}}
                                 .Root<Func<INavigation, Context0, Screen0>>("Root");
                     }

                     public static class Program
                     {
                         public static void Main() =>
                             Console.WriteLine(new Composition().Root(new Navigation(0), new Context0("start")).Run(0, {{RingOfDelegatesSteps}}));
                     }
                 }
                 """;
    }

    // Screens that open each other through delegates are each created in one place when they use an instance
    // that a factory creates with a PerBlock dependency and that a singleton outside the screens uses too, see #195.
    // The variants change what the factory injects, how the instance reaches the screens and who else uses it.
    private static readonly int[] SharedFactoryRingSizes = [4, 8, 12];

    [Theory]
    // IStorage is created by a factory that injects Func<IClock>, and the singleton Hub uses IStorage too.
    [InlineData(SharedFactoryRing.FuncFactory)]
    // IStorage is read by a factory from a singleton module that gets Func<IClock>, and the singleton Hub uses IStorage too.
    [InlineData(SharedFactoryRing.Module)]
    // The module variant without Hub.
    [InlineData(SharedFactoryRing.ModuleWithoutHub)]
    // IStorage is created by its constructor from the singleton module, and Hub uses IStorage.
    [InlineData(SharedFactoryRing.ModuleStorage)]
    // IStorage is created by its constructor, and Hub uses IStorage.
    [InlineData(SharedFactoryRing.Constructor)]
    // The factory injects Lazy<IClock> instead of Func<IClock>.
    [InlineData(SharedFactoryRing.LazyFactory)]
    // The factory injects Func<IClock> only in a branch.
    [InlineData(SharedFactoryRing.ConditionalFactory)]
    // The factory of IStorage injects ICache, which another factory creates with Func<IClock>.
    [InlineData(SharedFactoryRing.NestedFactories)]
    // IStorage is a PerBlock instance of the factory, so each block of a screen creates it once.
    [InlineData(SharedFactoryRing.PerBlockFactory)]
    // IClock is a PerBlock instance that the factory injects directly, and the screens get it too.
    // The factory needs the clock of the block around it, so the screens are still created in place on each path.
    [InlineData(SharedFactoryRing.PerBlockClock, Skip = "The screens are created in place on each path when a factory that a singleton also uses injects a PerBlock instance directly")]
    // Hub is scoped instead of a singleton.
    [InlineData(SharedFactoryRing.ScopedHub)]
    // Hub is a singleton created by a factory that injects IStorage.
    [InlineData(SharedFactoryRing.FactoryHub)]
    // Hub is transient, and both the root and the screens get it, so IStorage is also used by a transient outside the screens.
    [InlineData(SharedFactoryRing.TransientHub)]
    // Only the root gets the singleton Hub, so the screens never reach it.
    [InlineData(SharedFactoryRing.HubOnlyInRoot)]
    // The module that gets Func<IClock> and Hub are scoped instead of singletons.
    [InlineData(SharedFactoryRing.ScopedModule)]
    public async Task ShouldCreateEachScreenOnCycleOnceWhenSingletonSharesFactoryInstance(SharedFactoryRing variant)
    {
        // Given

        foreach (var size in SharedFactoryRingSizes)
        {
            // When
            var result = await CreateSharedFactoryRing(size, variant).RunAsync(new Options(Options.ModernLanguageVersion));

            // Then
            result.Success.ShouldBeTrue(result);
            result.StdOut.ShouldBe([GetExpectedSharedFactoryRingPath(size, variant)], result);
            var lines = result.GeneratedCode.Split(Environment.NewLine);
            for (var screen = 0; screen < size; screen++)
            {
                lines.Count(i => i.Contains($"new global::Sample.Screen{screen}(")).ShouldBe(1, $"Screen{screen} of {size} screens{Environment.NewLine}{result.GeneratedCode}");
            }
        }
    }

    // The factory of a transient instance that injects a PerBlock delegate is called in the block of each screen,
    // so each screen gets an IStorage of its own, while the local functions of the screens share the singleton Hub.
    [Theory]
    [InlineData(SharedFactoryRing.FuncFactory)]
    [InlineData(SharedFactoryRing.LazyFactory)]
    [InlineData(SharedFactoryRing.ConditionalFactory)]
    [InlineData(SharedFactoryRing.NestedFactories)]
    [InlineData(SharedFactoryRing.PerBlockClock)]
    [InlineData(SharedFactoryRing.FactoryHub)]
    public async Task ShouldCreateFactoryInstanceForEachScreenOnCycleAndShareSingleton(SharedFactoryRing variant)
    {
        // Given
        const string main =
            """
            IScreen screen = new Composition().Root(new Navigation(0), new Context0("start"));
            var screens = new System.Collections.Generic.List<IScreen>();
            for (var step = 0; step < 6; step++)
            {
                screens.Add(screen);
                screen = screen.Open(step);
            }

            Console.WriteLine(screens.Select(i => i.Storage).Distinct().Count());
            Console.WriteLine(screens.Select(i => i.Hub).Distinct().Count());
            """;

        // When
        var result = await CreateSharedFactoryRing(4, variant, main).RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["6", "1"], result);
    }

    public enum SharedFactoryRing
    {
        FuncFactory,
        Module,
        ModuleWithoutHub,
        ModuleStorage,
        Constructor,
        LazyFactory,
        ConditionalFactory,
        NestedFactories,
        PerBlockFactory,
        PerBlockClock,
        ScopedHub,
        FactoryHub,
        TransientHub,
        HubOnlyInRoot,
        ScopedModule
    }

    // Each screen opens the next two, so the path visits the screens by steps of one and two.
    private static string GetExpectedSharedFactoryRingPath(int size, SharedFactoryRing variant)
    {
        var storage = variant == SharedFactoryRing.PerBlockClock ? "storage,same" : "storage";
        var path = new StringBuilder();
        var screen = 0;
        var context = "start";
        for (var step = 0; step < SharedFactoryRingSteps; step++)
        {
            path.Append($"S{screen}({context},N{step},{storage}) ");
            var next = (screen + 1 + step % 2) % size;
            context = $"c{step}";
            screen = next;
        }

        path.Append($"S{screen}({context},N{SharedFactoryRingSteps},{storage})");
        return path.ToString();
    }

    private const int SharedFactoryRingSteps = 7;

    private static string CreateSharedFactoryRing(int size, SharedFactoryRing variant, string? main = null)
    {
        var hubLifetime = variant switch
        {
            SharedFactoryRing.ModuleWithoutHub => null,
            SharedFactoryRing.ScopedHub or SharedFactoryRing.ScopedModule => "Scoped",
            SharedFactoryRing.TransientHub => "Transient",
            _ => "Singleton"
        };

        var hubInScreens = hubLifetime is not null && variant != SharedFactoryRing.HubOnlyInRoot;
        var hubInRoot = variant is SharedFactoryRing.TransientHub or SharedFactoryRing.HubOnlyInRoot;
        var screenGetsClock = variant == SharedFactoryRing.PerBlockClock;
        var classes = new StringBuilder();
        for (var i = 0; i < size; i++)
        {
            var a = (i + 1) % size;
            var b = (i + 2) % size;
            var hubParameter = hubInScreens ? "Hub hub," : "";
            var hubAssignment = hubInScreens ? "Hub = hub;" : "";
            var clockParameter = screenGetsClock ? "IClock clock," : "";
            var clockAssignment = screenGetsClock ? "_clock = clock;" : "";
            var clockField = screenGetsClock ? "private readonly IClock _clock;" : "";
            var clockSuffix = screenGetsClock ? """,{(((ClockStorage)Storage).Clock == _clock ? "same" : "other")}""" : "";
            classes.AppendLine(
                $$"""
                      public sealed class Context{{i}}
                      {
                          public Context{{i}}(string value) => Value = value;

                          public string Value { get; }
                      }

                      public sealed class Screen{{i}} : IScreen
                      {
                          private readonly Context{{i}} _context;
                          private readonly INavigation _navigation;
                          {{clockField}}
                          private readonly Func<INavigation, Context{{a}}, Screen{{a}}> _openA;
                          private readonly Func<INavigation, Context{{b}}, Screen{{b}}> _openB;

                          public Screen{{i}}(
                              Context{{i}} context,
                              INavigation navigation,
                              IStorage storage,
                              {{hubParameter}}
                              {{clockParameter}}
                              Func<INavigation, Context{{a}}, Screen{{a}}> openA,
                              Func<INavigation, Context{{b}}, Screen{{b}}> openB)
                          {
                              _context = context;
                              _navigation = navigation;
                              Storage = storage;
                              {{hubAssignment}}
                              {{clockAssignment}}
                              _openA = openA;
                              _openB = openB;
                          }

                          public IStorage Storage { get; }

                          public Hub? Hub { get; }

                          public IScreen Open(int step)
                          {
                              var navigation = new Navigation(step + 1);
                              return step % 2 == 0
                                  ? _openA(navigation, new Context{{a}}($"c{step}"))
                                  : _openB(navigation, new Context{{b}}($"c{step}"));
                          }

                          public string Run(int step, int steps)
                          {
                              var current = $"S{{i}}({_context.Value},{_navigation},{Storage}{{clockSuffix}})";
                              return step == steps ? current : current + " " + Open(step).Run(step + 1, steps);
                          }
                      }

                  """);
        }

        const string funcFactory =
            """
            .Bind<IStorage>().To(ctx =>
            {
                ctx.Inject(out Func<IClock> clock);
                return new Storage();
            })
            """;

        const string moduleFactory =
            """
            .Bind<IStorage>().To(ctx =>
            {
                ctx.Inject(out Module module);
                return module.Storage;
            })
            """;

        var storageBindings = variant switch
        {
            SharedFactoryRing.Module or SharedFactoryRing.ModuleWithoutHub =>
                ".Bind().As(Lifetime.Singleton).To<Module>()" + moduleFactory,
            SharedFactoryRing.ScopedModule =>
                ".Bind().As(Lifetime.Scoped).To<Module>()" + moduleFactory,
            SharedFactoryRing.ModuleStorage =>
                """
                .Bind().As(Lifetime.Singleton).To<Module>()
                .Bind<IStorage>().To<ModuleStorage>()
                """,
            SharedFactoryRing.Constructor =>
                ".Bind<IStorage>().To<Storage>()",
            SharedFactoryRing.LazyFactory =>
                """
                .Bind<IStorage>().To(ctx =>
                {
                    ctx.Inject(out Lazy<IClock> clock);
                    return new Storage();
                })
                """,
            SharedFactoryRing.ConditionalFactory =>
                """
                .Bind<IStorage>().To(ctx =>
                {
                    if (Environment.ProcessorCount > 0)
                    {
                        ctx.Inject(out Func<IClock> clock);
                    }

                    return new Storage();
                })
                """,
            SharedFactoryRing.NestedFactories =>
                """
                .Bind<ICache>().To(ctx =>
                {
                    ctx.Inject(out Func<IClock> clock);
                    return new Cache();
                })
                .Bind<IStorage>().To(ctx =>
                {
                    ctx.Inject(out ICache cache);
                    return new Storage();
                })
                """,
            SharedFactoryRing.PerBlockFactory =>
                """
                .Bind<IStorage>().As(Lifetime.PerBlock).To(ctx =>
                {
                    ctx.Inject(out Func<IClock> clock);
                    return new Storage();
                })
                """,
            SharedFactoryRing.PerBlockClock =>
                """
                .Bind<IStorage>().To(ctx =>
                {
                    ctx.Inject(out IClock clock);
                    return new ClockStorage(clock);
                })
                """,
            _ => funcFactory
        };

        var clockBinding = variant switch
        {
            SharedFactoryRing.Constructor => "",
            SharedFactoryRing.PerBlockClock => ".Bind<IClock>().As(Lifetime.PerBlock).To<Clock>()",
            _ => ".Bind<IClock>().To<Clock>()"
        };

        var hubBinding = variant switch
        {
            _ when hubLifetime is null => "",
            SharedFactoryRing.FactoryHub =>
                """
                .Bind().As(Lifetime.Singleton).To(ctx =>
                {
                    ctx.Inject(out IStorage storage);
                    return new Hub(storage);
                })
                """,
            _ => $".Bind().As(Lifetime.{hubLifetime}).To<Hub>()"
        };

        var root = hubInRoot
            ? ".Root<App>(\"Root\")"
            : ".Root<Func<INavigation, Context0, Screen0>>(\"Root\")";

        var open = hubInRoot ? "Root.Open" : "Root";
        main ??= $"Console.WriteLine(new Composition().{open}(new Navigation(0), new Context0(\"start\")).Run(0, {SharedFactoryRingSteps}));";

        return $$"""
                 using System;
                 using System.Linq;
                 using Pure.DI;

                 namespace Sample
                 {
                     public interface INavigation { }

                     public sealed class Navigation : INavigation
                     {
                         private readonly int _id;

                         public Navigation(int id) => _id = id;

                         public override string ToString() => $"N{_id}";
                     }

                     public interface IClock { }

                     public sealed class Clock : IClock { }

                     public interface ICache { }

                     public sealed class Cache : ICache { }

                     public interface IStorage { }

                     public sealed class Storage : IStorage
                     {
                         public override string ToString() => "storage";
                     }

                     public sealed class ClockStorage : IStorage
                     {
                         public ClockStorage(IClock clock) => Clock = clock;

                         public IClock Clock { get; }

                         public override string ToString() => "storage";
                     }

                     public sealed class Module
                     {
                         public Module(Func<IClock> clock) { }

                         public IStorage Storage { get; } = new Storage();
                     }

                     public sealed class ModuleStorage : IStorage
                     {
                         public ModuleStorage(Module module) { }

                         public override string ToString() => "storage";
                     }

                     public sealed class Hub
                     {
                         public Hub(IStorage storage) => Storage = storage;

                         public IStorage Storage { get; }
                     }

                     public sealed class App
                     {
                         public App(Hub hub, Func<INavigation, Context0, Screen0> open) => Open = open;

                         public Func<INavigation, Context0, Screen0> Open { get; }
                     }

                     public interface IScreen
                     {
                         IStorage Storage { get; }

                         Hub? Hub { get; }

                         IScreen Open(int step);

                         string Run(int step, int steps);
                     }

                 {{classes}}
                     partial class Composition
                     {
                         private static void Setup() =>
                             DI.Setup(nameof(Composition))
                                 {{clockBinding}}
                                 {{hubBinding}}
                                 {{storageBindings}}
                                 {{root}};
                     }

                     public static class Program
                     {
                         public static void Main()
                         {
                             {{main}}
                         }
                     }
                 }
                 """;
    }

    private async Task CheckGrowth(int[] sizes, Func<int, Ring> createRing)
    {
        // All the sizes are measured before the checks, so a failure shows the growth too.
        var measurements = new List<Measurement>();
        var problems = new List<string>();
        foreach (var size in sizes)
        {
            var ring = createRing(size);
            var stopwatch = Stopwatch.StartNew();
            Result result;
            try
            {
                result = await ring.Create().RunAsync(new Options(Options.ModernLanguageVersion), GenerationTimeout);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"{size} screens are not generated in {GenerationTimeout}; previous: {string.Join("; ", measurements)}");
            }

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

        // The lifetime of the service that the header and the body of a screen share.
        public string ServicesLifetime { get; init; } = "Transient";

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

            if (HasDiamond && ServicesLifetime != "Transient")
            {
                code.AppendLine($"            .Bind<Services>().As(Lifetime.{ServicesLifetime}).To<Services>()");
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
