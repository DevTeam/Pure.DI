namespace Pure.DI.IntegrationTests;

using System.Text;

/// <summary>
/// Checks the behavior of the graphs whose code the growth tests made smaller, see #192.
/// The growth tests check the values that the screens read, while these tests check the instances that the code shares,
/// creates once, disposes and creates only where a factory injects them.
/// </summary>
public class LocalFunctionCorrectnessTests
{
    private const int Steps = 9;

    // Each level reads a value overridden by a lambda and gets a PerBlock instance that reads it too, and the level below it,
    // both through Func<T1, T> that overrides the value and directly. A level and the level it gets directly
    // are created in the same block, so they share the instance, while the code of the lambda creates it anew.
    [Theory]
    [InlineData("Transient", "Auto")]
    [InlineData("Transient", "On")]
    [InlineData("PerBlock", "Auto")]
    [InlineData("PerBlock", "On")]
    public async Task ShouldShareInstanceReadingOverriddenValueInItsBlockOnly(string lifetime, string mode)
    {
        // Given
        const int depth = 10;
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        for (var level = 1; level <= depth; level++)
        {
            classes.AppendLine($$"""
                sealed class N{{level}} : INode
                {
                    private readonly Func<Ctx, N{{level - 1}}> _open;
                    private readonly N{{level - 1}} _direct;
                    private readonly bool _readsSameCtx;
                    public N{{level}}(Ctx ctx, Shared shared, Func<Ctx, N{{level - 1}}> open, N{{level - 1}} direct) { Shared = shared; _open = open; _direct = direct; _readsSameCtx = ReferenceEquals(ctx, shared.Ctx) || ctx.Value == shared.Ctx.Value; }
                    public Shared Shared { get; }
                    public string Walk(int mask)
                    {
                        if (((mask >> {{level}}) & 1) == 1)
                        {
                            var child = _open(new Ctx("{{level}}"));
                            var isNew = !ReferenceEquals(child.Shared, Shared) && child.Shared.Ctx.Value == "{{level}}";
                            return Shared.Ctx.Value + (isNew && _readsSameCtx ? "" : "!") + " " + child.Walk(mask);
                        }

                        return Shared.Ctx.Value + (ReferenceEquals(_direct.Shared, Shared) && _readsSameCtx ? "" : "!") + " " + _direct.Walk(mask);
                    }
                }
                """);
            if (lifetime != "Transient")
            {
                bindings.AppendLine($".Bind<N{level}>().As(Lifetime.{lifetime}).To<N{level}>()");
            }
        }

        var masks = GetMasks(depth);

        // When
        var result = await $$"""
            using System;
            using Pure.DI;

            namespace Sample
            {
                interface INode { Shared Shared { get; } string Walk(int mask); }
                sealed class Ctx { public Ctx(string value) { Value = value; } public string Value { get; } }
                sealed class Shared { public Shared(Ctx ctx) { Ctx = ctx; } public Ctx Ctx { get; } }
                sealed class Left { public Left(Shared shared) { Shared = shared; } public Shared Shared { get; } }
                sealed class Right { public Right(Shared shared) { Shared = shared; } public Shared Shared { get; } }
                sealed class N0 : INode
                {
                    private readonly bool _isShared;
                    public N0(Left left, Right right) { Shared = left.Shared; _isShared = ReferenceEquals(left.Shared, right.Shared); }
                    public Shared Shared { get; }
                    public string Walk(int mask) => Shared.Ctx.Value + (_isShared ? "" : "!");
                }
            {{classes}}
                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Hint(Hint.LocalFunctions, "{{mode}}")
                        .Bind<Ctx>().To(_ => new Ctx("d"))
                        .Bind<Shared>().As(Lifetime.PerBlock).To<Shared>()
            {{bindings}}
                        .Root<N{{depth}}>("Root");
                }

                public static class Program
                {
                    public static void Main()
                    {
                        var root = new Composition().Root;
                        Console.WriteLine({{string.Join(" + \" | \" + ", masks.Select(mask => $"root.Walk({mask})"))}});
                    }
                }
            }
            """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([string.Join(" | ", masks.Select(mask => GetExpectedWalk(depth, mask)))], result);
    }

    // The screens of a branching cycle are shared, and several threads open them at once.
    // The code checks whether a shared screen exists in the local function of each screen, and each screen is created once.
    [Theory]
    [InlineData("Singleton", "Lazy", "Auto")]
    [InlineData("Singleton", "Lazy", "On")]
    [InlineData("Singleton", "Func", "Auto")]
    [InlineData("Scoped", "Lazy", "Auto")]
    [InlineData("Scoped", "Func", "On")]
    public async Task ShouldCreateEachSharedScreenOfBranchingCycleOnceForSeveralThreads(string lifetime, string edge, string mode)
    {
        // Given
        const int size = 8;
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        for (var screen = 0; screen < size; screen++)
        {
            var first = (screen + 1) % size;
            var second = (screen + 2) % size;
            var type = edge == "Lazy" ? "Lazy<Screen{0}>" : "Func<Screen{0}>";
            var call = edge == "Lazy" ? "{0}.Value" : "{0}()";
            classes.AppendLine($$"""
                sealed class Screen{{screen}} : IScreen
                {
                    private readonly {{string.Format(type, first)}} _first;
                    private readonly {{string.Format(type, second)}} _second;
                    public Screen{{screen}}({{string.Format(type, first)}} first, {{string.Format(type, second)}} second)
                    {
                        _first = first;
                        _second = second;
                        Interlocked.Increment(ref Counter.Created[{{screen}}]);
                    }
                    public string Run(int step, int steps) =>
                        step == steps ? "S{{screen}}" : "S{{screen}} " + (step % 2 == 0 ? (IScreen){{string.Format(call, "_first")}} : {{string.Format(call, "_second")}}).Run(step + 1, steps);
                }
                """);
            bindings.AppendLine($".Bind<Screen{screen}>().As(Lifetime.{lifetime}).To<Screen{screen}>()");
        }

        // When
        var result = await $$"""
            using System;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using Pure.DI;

            namespace Sample
            {
                interface IScreen { string Run(int step, int steps); }
                static class Counter { public static readonly int[] Created = new int[{{size}}]; }
            {{classes}}
                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Hint(Hint.LocalFunctions, "{{mode}}")
                        .Hint(Hint.ThreadSafe, "On")
            {{bindings}}
                        .Root<Screen0>("Root");
                }

                public static class Program
                {
                    public static void Main()
                    {
                        var composition = new Composition();
                        var start = new ManualResetEventSlim();
                        var runs = Enumerable.Range(0, 8).Select(_ => Task.Run(() => { start.Wait(); return composition.Root.Run(0, 20); })).ToArray();
                        start.Set();
                        Task.WaitAll(runs);
                        Console.WriteLine($"{runs.Select(i => i.Result).Distinct().Count()} {string.Join(",", Counter.Created)}");
                    }
                }
            }
            """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([$"1 {string.Join(",", Enumerable.Repeat(1, size))}"], result);
    }

    // Each screen of a cycle is opened as Owned<T> and is disposed after it runs, together with its disposable dependency.
    // A header that reads the title which the factory of a screen overrides is not disposable, so its local function
    // is declared at the level of the root and is shared by the screens. Each disposable instance is disposed once,
    // and only when the Owned<T> that created it is disposed.
    [Theory]
    [InlineData("Transient", true, "Auto")]
    [InlineData("Transient", true, "On")]
    [InlineData("Transient", false, "Auto")]
    [InlineData("PerBlock", true, "Auto")]
    [InlineData("PerBlock", false, "On")]
    public async Task ShouldDisposeInstancesOfEachOwnedScreenOfCycleOnce(string lifetime, bool hasFactory, string mode)
    {
        // Given
        const int size = 6;
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        for (var screen = 0; screen < size; screen++)
        {
            var first = (screen + 1) % size;
            var second = (screen + 2) % size;
            classes.AppendLine($$"""
                sealed class Screen{{screen}} : IScreen, IDisposable
                {
                    private readonly Header _header;
                    private readonly Func<Owned<Screen{{first}}>> _first;
                    private readonly Func<Owned<Screen{{second}}>> _second;
                    private bool _isDisposed;
                    public Screen{{screen}}(Header header, Dep dep, Func<Owned<Screen{{first}}>> first, Func<Owned<Screen{{second}}>> second)
                    {
                        _header = header;
                        _first = first;
                        _second = second;
                        Live.Count++;
                    }
                    public string Run(int step, int steps)
                    {
                        var current = $"S{{screen}}({_header.Title.Value})";
                        if (step == steps) return current;
                        var before = Live.Count;
                        string next;
                        if (step % 2 == 0)
                        {
                            var owned = _first();
                            next = owned.Value.Run(step + 1, steps);
                            owned.Dispose();
                        }
                        else
                        {
                            var owned = _second();
                            next = owned.Value.Run(step + 1, steps);
                            owned.Dispose();
                        }

                        if (Live.Count != before) Live.Errors++;
                        return current + " " + next;
                    }
                    public void Dispose() { if (_isDisposed) Live.Errors++; _isDisposed = true; Live.Count--; }
                }
                """);
            if (hasFactory)
            {
                bindings.AppendLine($$"""
                    .Bind<Screen{{screen}}>().As(Lifetime.{{lifetime}}).To(ctx =>
                    {
                        ctx.Override(new Title("T{{screen}}"));
                        ctx.Inject(out Header header);
                        ctx.Inject(out Dep dep);
                        ctx.Inject(out Func<Owned<Screen{{first}}>> first);
                        ctx.Inject(out Func<Owned<Screen{{second}}>> second);
                        return new Screen{{screen}}(header, dep, first, second);
                    })
                    """);
            }
            else if (lifetime != "Transient")
            {
                bindings.AppendLine($".Bind<Screen{screen}>().As(Lifetime.{lifetime}).To<Screen{screen}>()");
            }
        }

        // When
        var result = await $$"""
            using System;
            using Pure.DI;

            namespace Sample
            {
                interface IScreen { string Run(int step, int steps); }
                static class Live { public static int Count; public static int Errors; }
                sealed class Title { public Title(string value) { Value = value; } public string Value { get; } }
                sealed class Header { public Header(Title title) { Title = title; } public Title Title { get; } }
                sealed class Dep : IDisposable
                {
                    private bool _isDisposed;
                    public Dep() { Live.Count++; }
                    public void Dispose() { if (_isDisposed) Live.Errors++; _isDisposed = true; Live.Count--; }
                }
            {{classes}}
                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Hint(Hint.LocalFunctions, "{{mode}}")
                        .Bind<Title>().To(_ => new Title("default"))
            {{bindings}}
                        .Root<Func<Owned<Screen0>>>("Root");
                }

                public static class Program
                {
                    public static void Main()
                    {
                        var owned = new Composition().Root();
                        var path = owned.Value.Run(0, {{Steps}});
                        owned.Dispose();
                        Console.WriteLine($"{path} {Live.Count} {Live.Errors}");
                    }
                }
            }
            """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        var expected = string.Join(" ", GetRingPath(size).Select(screen => $"S{screen}({(hasFactory ? $"T{screen}" : "default")})"));
        result.StdOut.ShouldBe([$"{expected} 0 0"], result);
    }

    // The factory of each screen of a cycle injects a PerBlock tracker only in a branch. The local function of a screen
    // must not create the tracker before the factory decides to inject it, and creates it once in the block of each screen.
    [Theory]
    [InlineData("Transient", "Auto")]
    [InlineData("Transient", "On")]
    [InlineData("PerBlock", "Auto")]
    [InlineData("PerBlock", "On")]
    public async Task ShouldInjectDependencyOfBranchOfFactoryOnCycleOnlyInThisBranch(string lifetime, string mode)
    {
        // Given
        const int size = 6;
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        for (var screen = 0; screen < size; screen++)
        {
            var first = (screen + 1) % size;
            var second = (screen + 2) % size;
            classes.AppendLine($$"""
                sealed class Screen{{screen}} : IScreen
                {
                    private readonly Func<Screen{{first}}> _first;
                    private readonly Func<Screen{{second}}> _second;
                    private readonly bool _hasTracker;
                    public Screen{{screen}}(Func<Screen{{first}}> first, Func<Screen{{second}}> second, Tracker? tracker)
                    {
                        _first = first;
                        _second = second;
                        _hasTracker = tracker != null;
                    }
                    public string Run(int step, int steps)
                    {
                        var current = "S{{screen}}" + (_hasTracker ? "+" : "");
                        return step == steps ? current : current + " " + (step % 2 == 0 ? (IScreen)_first() : _second()).Run(step + 1, steps);
                    }
                }
                """);
            bindings.AppendLine($$"""
                .Bind<Screen{{screen}}>().As(Lifetime.{{lifetime}}).To(ctx =>
                {
                    ctx.Inject(out Func<Screen{{first}}> first);
                    ctx.Inject(out Func<Screen{{second}}> second);
                    if (Config.UseTracker)
                    {
                        ctx.Inject(out Tracker tracker);
                        return new Screen{{screen}}(first, second, tracker);
                    }

                    return new Screen{{screen}}(first, second, null);
                })
                """);
        }

        // When
        var result = await $$"""
            using System;
            using Pure.DI;

            namespace Sample
            {
                interface IScreen { string Run(int step, int steps); }
                static class Config { public static bool UseTracker; }
                sealed class Tracker { public static int Created; public Tracker() { Created++; } }
            {{classes}}
                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Hint(Hint.LocalFunctions, "{{mode}}")
                        .Bind<Tracker>().As(Lifetime.PerBlock).To<Tracker>()
            {{bindings}}
                        .Root<Func<Screen0>>("Root");
                }

                public static class Program
                {
                    public static void Main()
                    {
                        var withoutTracker = new Composition().Root().Run(0, {{Steps}});
                        var created = Tracker.Created;
                        Config.UseTracker = true;
                        var withTracker = new Composition().Root().Run(0, {{Steps}});
                        Console.WriteLine($"{withoutTracker} {created} {withTracker} {Tracker.Created}");
                    }
                }
            }
            """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        var path = GetRingPath(size).ToList();
        result.StdOut.ShouldBe([$"{string.Join(" ", path.Select(i => $"S{i}"))} 0 {string.Join(" ", path.Select(i => $"S{i}+"))} {path.Count}"], result);
    }

    // A logger is created for the screen it is injected into. The screens of a cycle are created by local functions
    // that the lambdas call, and each screen still gets a logger of its own type.
    [Theory]
    [InlineData(false, "Auto")]
    [InlineData(false, "On")]
    [InlineData(true, "Auto")]
    [InlineData(true, "On")]
    public async Task ShouldPassConsumerTypeToEachScreenOfCycle(bool hasArgument, string mode)
    {
        // Given
        const int size = 6;
        var classes = new StringBuilder();
        for (var screen = 0; screen < size; screen++)
        {
            var first = (screen + 1) % size;
            var second = (screen + 2) % size;
            var firstType = hasArgument ? $"Func<Context, Screen{first}>" : $"Func<Screen{first}>";
            var secondType = hasArgument ? $"Func<Context, Screen{second}>" : $"Func<Screen{second}>";
            var arguments = hasArgument ? "new Context(step)" : "";
            classes.AppendLine($$"""
                sealed class Screen{{screen}} : IScreen
                {
                    private readonly Logger _logger;
                    private readonly {{firstType}} _first;
                    private readonly {{secondType}} _second;
                    public Screen{{screen}}(Logger logger, {{firstType}} first, {{secondType}} second) { _logger = logger; _first = first; _second = second; }
                    public string Run(int step, int steps)
                    {
                        var current = "S{{screen}}" + (_logger.Type == GetType() ? "" : "!" + _logger.Type.Name);
                        return step == steps ? current : current + " " + (step % 2 == 0 ? (IScreen)_first({{arguments}}) : _second({{arguments}})).Run(step + 1, steps);
                    }
                }
                """);
        }

        // When
        var result = await $$"""
            using System;
            using Pure.DI;

            namespace Sample
            {
                interface IScreen { string Run(int step, int steps); }
                sealed class Context { public Context(int step) { Step = step; } public int Step { get; } }
                sealed class Logger { public Logger(Type type) { Type = type; } public Type Type { get; } }
            {{classes}}
                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Hint(Hint.LocalFunctions, "{{mode}}")
                        .Bind<Logger>().To(ctx => new Logger(ctx.ConsumerType))
                        .Root<Screen0>("Root");
                }

                public static class Program
                {
                    public static void Main() => Console.WriteLine(new Composition().Root.Run(0, {{Steps}}));
                }
            }
            """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([string.Join(" ", GetRingPath(size).Select(i => $"S{i}"))], result);
    }

    // A wrapper of a screen reads the tag of its injection as a value, so its code depends on the tag,
    // and the wrappers for different tags do not share a local function, although they lead to the same screens.
    [Theory]
    [InlineData("Auto")]
    [InlineData("On")]
    public async Task ShouldKeepTagReadByWrapperOfScreenOfCycle(string mode)
    {
        // Given
        const int size = 6;
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        for (var screen = 0; screen < size; screen++)
        {
            var first = (screen + 1) % size;
            var second = (screen + 2) % size;
            classes.AppendLine($$"""
                sealed class Screen{{screen}} : IScreen
                {
                    private readonly Wrapper<Screen{{first}}> _first;
                    private readonly Wrapper<Screen{{second}}> _second;
                    public Screen{{screen}}([Tag("a")] Wrapper<Screen{{first}}> first, [Tag("b")] Wrapper<Screen{{second}}> second) { _first = first; _second = second; }
                    public string Run(int step, int steps)
                    {
                        if (step == steps) return "S{{screen}}";
                        return step % 2 == 0
                            ? "S{{screen}} " + _first.Tag + " " + _first.Create().Run(step + 1, steps)
                            : "S{{screen}} " + _second.Tag + " " + _second.Create().Run(step + 1, steps);
                    }
                }
                """);
            bindings.AppendLine($".Bind<Screen{screen}>(\"a\", \"b\").To<Screen{screen}>()");
        }

        // When
        var result = await $$"""
            using System;
            using Pure.DI;

            namespace Sample
            {
                interface IScreen { string Run(int step, int steps); }
                sealed class Wrapper<T>
                {
                    private readonly Func<T> _factory;
                    public Wrapper(Func<T> factory, string tag) { _factory = factory; Tag = tag; }
                    public string Tag { get; }
                    public T Create() => _factory();
                }
            {{classes}}
                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Hint(Hint.LocalFunctions, "{{mode}}")
                        .Bind<Wrapper<TT>>().To(ctx =>
                        {
                            ctx.Inject<Func<TT>>(ctx.Tag, out var factory);
                            return new Wrapper<TT>(factory, ctx.Tag?.ToString() ?? "");
                        })
            {{bindings}}
                        .Root<Wrapper<Screen0>>("Root", "a");
                }

                public static class Program
                {
                    public static void Main()
                    {
                        var root = new Composition().Root;
                        Console.WriteLine(root.Tag + " " + root.Create().Run(0, {{Steps}}));
                    }
                }
            }
            """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        var path = GetRingPath(size).ToList();
        var expected = new StringBuilder("a");
        for (var step = 0; step < path.Count; step++)
        {
            expected.Append($" S{path[step]}");
            if (step < Steps)
            {
                expected.Append(step % 2 == 0 ? " a" : " b");
            }
        }

        result.StdOut.ShouldBe([expected.ToString()], result);
    }

    // The screens that a walk opens: each screen opens the next screen on even steps and the one after it on odd steps.
    private static IEnumerable<int> GetRingPath(int size)
    {
        var screen = 0;
        for (var step = 0; step <= Steps; step++)
        {
            yield return screen;
            screen = (screen + 1 + step % 2) % size;
        }
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

    // A level reads the value of the nearest delegate above it that overrides the value.
    private static string GetExpectedWalk(int depth, int mask)
    {
        var values = new List<string>();
        for (var level = depth; level >= 0; level--)
        {
            var nearest = Enumerable.Range(level + 1, depth - level).Where(i => ((mask >> i) & 1) == 1).Select(i => $"{i}").FirstOrDefault();
            values.Add(nearest ?? "d");
        }

        return string.Join(" ", values);
    }
}
