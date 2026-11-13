namespace Pure.DI.IntegrationTests;

/// <summary>
/// A local function generated in the code that creates a shared instance, which it reaches through a lambda,
/// can also be called by code that runs before that instance is created, see #202.
/// </summary>
public class SharedInstanceInCreationTests
{
    public static IEnumerable<object[]> Cases() =>
        from lifetime in new[] { "Singleton", "Scoped" }
        from localFunctions in new[] { "Auto", "On", "Off" }
        select new object[] { lifetime, localFunctions };

    // The singleton that reads the exported member is created by a lambda of the root before the module exists.
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ShouldCreateExportingInstanceWhenTheInstanceThatUsesItIsCreatedFirst(string lifetime, string localFunctions)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;
                             using static Pure.DI.Lifetime;

                             namespace Sample;

                             public interface IService { }
                             public sealed class Service : IService { }

                             public sealed class Module
                             {
                                 public static int Count;

                                 public Module(Func<Logger> logger) => Count++;

                                 [Export]
                                 public IService Service { get; } = new Service();
                             }

                             public sealed class Logger
                             {
                                 public Logger(IService service) =>
                                     Service = service ?? throw new InvalidOperationException("Service was null");

                                 public IService Service { get; }
                             }

                             public sealed class App
                             {
                                 private readonly Func<Module> _module;
                                 private readonly Func<Logger> _logger;

                                 public App(Func<Module> module, Func<Logger> logger)
                                 {
                                     _module = module;
                                     _logger = logger;
                                 }

                                 public Module Module() => _module();

                                 public Logger Logger() => _logger();
                             }

                             partial class Composition
                             {
                                 private static void Setup() => DI.Setup(nameof(Composition))
                                     .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                                     .Bind().As({{lifetime}}).To<Module>()
                                     .Bind().As({{lifetime}}).To<Logger>()
                                     .Root<App>("Root");
                             }

                             class Program
                             {
                                 static void Main()
                                 {
                                     var composition = new Composition();
                                     var app = composition.Root;
                                     var logger = app.Logger();
                                     Console.WriteLine(ReferenceEquals(logger, app.Logger()));
                                     Console.WriteLine(ReferenceEquals(app.Module().Service, logger.Service));
                                     Console.WriteLine(Module.Count);
                                 }
                             }
                             """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True", "True", "1"]);
        result.Success.ShouldBeTrue(result);
    }

    // The module is created first, so the singleton created later reads the member of the same module.
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ShouldReuseExportingInstanceWhenItIsCreatedFirst(string lifetime, string localFunctions)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;
                             using static Pure.DI.Lifetime;

                             namespace Sample;

                             public interface IService { }
                             public sealed class Service : IService { }

                             public sealed class Module
                             {
                                 public static int Count;

                                 private readonly Func<Logger> _logger;

                                 public Module(Func<Logger> logger)
                                 {
                                     _logger = logger;
                                     Count++;
                                 }

                                 [Export]
                                 public IService Service { get; } = new Service();

                                 public Logger Logger() => _logger();
                             }

                             public sealed class Logger
                             {
                                 public Logger(IService service) =>
                                     Service = service ?? throw new InvalidOperationException("Service was null");

                                 public IService Service { get; }
                             }

                             public sealed class App
                             {
                                 private readonly Func<Module> _module;
                                 private readonly Func<Logger> _logger;

                                 public App(Func<Module> module, Func<Logger> logger)
                                 {
                                     _module = module;
                                     _logger = logger;
                                 }

                                 public Module Module() => _module();

                                 public Logger Logger() => _logger();
                             }

                             partial class Composition
                             {
                                 private static void Setup() => DI.Setup(nameof(Composition))
                                     .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                                     .Bind().As({{lifetime}}).To<Module>()
                                     .Bind().As({{lifetime}}).To<Logger>()
                                     .Root<App>("Root");
                             }

                             class Program
                             {
                                 static void Main()
                                 {
                                     var composition = new Composition();
                                     var app = composition.Root;
                                     var module = app.Module();
                                     var logger = module.Logger();
                                     Console.WriteLine(ReferenceEquals(logger, app.Logger()));
                                     Console.WriteLine(ReferenceEquals(module.Service, logger.Service));
                                     Console.WriteLine(Module.Count);
                                 }
                             }
                             """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True", "True", "1"]);
        result.Success.ShouldBeTrue(result);
    }

    // The singleton that reads the module is created by Lazy<T> instead of Func<T>.
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ShouldCreateInstanceInCreationWhenALazyLeadsBackToIt(string lifetime, string localFunctions)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;
                             using static Pure.DI.Lifetime;

                             namespace Sample;

                             public sealed class Module
                             {
                                 public Module(Lazy<Logger> logger) { }
                             }

                             public sealed class Logger
                             {
                                 public Logger(Module module) =>
                                     Module = module ?? throw new InvalidOperationException("Module was null");

                                 public Module Module { get; }
                             }

                             public sealed class App
                             {
                                 private readonly Lazy<Logger> _logger;

                                 public App(Lazy<Module> module, Lazy<Logger> logger) => _logger = logger;

                                 public Logger Logger() => _logger.Value;
                             }

                             partial class Composition
                             {
                                 private static void Setup() => DI.Setup(nameof(Composition))
                                     .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                                     .Bind().As({{lifetime}}).To<Module>()
                                     .Bind().As({{lifetime}}).To<Logger>()
                                     .Root<App>("Root");
                             }

                             class Program
                             {
                                 static void Main()
                                 {
                                     var composition = new Composition();
                                     var logger = composition.Root.Logger();
                                     Console.WriteLine(ReferenceEquals(logger.Module, composition.Root.Logger().Module));
                                 }
                             }
                             """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True"]);
        result.Success.ShouldBeTrue(result);
    }

    // A transient instance between the lambda and the singleton that reads the module.
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ShouldCreateInstanceInCreationWhenATransientLeadsBackToIt(string lifetime, string localFunctions)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;
                             using static Pure.DI.Lifetime;

                             namespace Sample;

                             public sealed class Module
                             {
                                 public static int Count;

                                 public Module(Func<Consumer> consumer) => Count++;
                             }

                             public sealed class Logger
                             {
                                 public Logger(Module module) =>
                                     Module = module ?? throw new InvalidOperationException("Module was null");

                                 public Module Module { get; }
                             }

                             public sealed class Consumer
                             {
                                 public Consumer(Logger logger, Module module)
                                 {
                                     Logger = logger;
                                     Module = module ?? throw new InvalidOperationException("Module was null");
                                 }

                                 public Logger Logger { get; }

                                 public Module Module { get; }
                             }

                             public sealed class App
                             {
                                 private readonly Func<Consumer> _consumer;

                                 public App(Func<Module> module, Func<Consumer> consumer) => _consumer = consumer;

                                 public Consumer Consumer() => _consumer();
                             }

                             partial class Composition
                             {
                                 private static void Setup() => DI.Setup(nameof(Composition))
                                     .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                                     .Bind().As({{lifetime}}).To<Module>()
                                     .Bind().As({{lifetime}}).To<Logger>()
                                     .Bind().To<Consumer>()
                                     .Root<App>("Root");
                             }

                             class Program
                             {
                                 static void Main()
                                 {
                                     var composition = new Composition();
                                     var first = composition.Root.Consumer();
                                     var second = composition.Root.Consumer();
                                     Console.WriteLine(ReferenceEquals(first.Module, first.Logger.Module));
                                     Console.WriteLine(ReferenceEquals(first.Module, second.Module));
                                     Console.WriteLine(Module.Count);
                                 }
                             }
                             """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True", "True", "1"]);
        result.Success.ShouldBeTrue(result);
    }

    // A chain of shared instances, each of which gets the next one by a lambda, and the last one reads the first one.
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ShouldCreateInstanceInCreationAtTheStartOfAChainOfLambdas(string lifetime, string localFunctions)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;
                             using static Pure.DI.Lifetime;

                             namespace Sample;

                             public sealed class First
                             {
                                 public static int Count;

                                 public First(Func<Second> second) => Count++;
                             }

                             public sealed class Second
                             {
                                 private readonly Func<Third> _third;

                                 public Second(Func<Third> third) => _third = third;

                                 public Third Third() => _third();
                             }

                             public sealed class Third
                             {
                                 public Third(First first) =>
                                     First = first ?? throw new InvalidOperationException("First was null");

                                 public First First { get; }
                             }

                             public sealed class App
                             {
                                 private readonly Func<Second> _second;
                                 private readonly Func<Third> _third;

                                 public App(Func<First> first, Func<Second> second, Func<Third> third)
                                 {
                                     _second = second;
                                     _third = third;
                                 }

                                 public Second Second() => _second();

                                 public Third Third() => _third();
                             }

                             partial class Composition
                             {
                                 private static void Setup() => DI.Setup(nameof(Composition))
                                     .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                                     .Bind().As({{lifetime}}).To<First>()
                                     .Bind().As({{lifetime}}).To<Second>()
                                     .Bind().As({{lifetime}}).To<Third>()
                                     .Root<App>("Root");
                             }

                             class Program
                             {
                                 static void Main()
                                 {
                                     var composition = new Composition();
                                     var app = composition.Root;
                                     var third = app.Second().Third();
                                     Console.WriteLine(ReferenceEquals(third, app.Third()));
                                     Console.WriteLine(third.First is not null);
                                     Console.WriteLine(First.Count);
                                 }
                             }
                             """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True", "True", "1"]);
        result.Success.ShouldBeTrue(result);
    }

    // The module is created by a factory, and the singleton that reads it is also a root of its own.
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ShouldCreateInstanceInCreationByFactoryWhenAnotherRootUsesIt(string lifetime, string localFunctions)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;
                             using static Pure.DI.Lifetime;

                             namespace Sample;

                             public sealed class Module
                             {
                                 public static int Count;

                                 public Module(Func<Logger> logger) => Count++;

                                 public string Name => "Module";
                             }

                             public sealed class Logger
                             {
                                 public Logger(Module module) =>
                                     Module = module ?? throw new InvalidOperationException("Module was null");

                                 public Module Module { get; }
                             }

                             public sealed class App
                             {
                                 private readonly Func<Logger> _logger;

                                 public App(Func<Module> module, Func<Logger> logger) => _logger = logger;

                                 public Logger Logger() => _logger();
                             }

                             partial class Composition
                             {
                                 private static void Setup() => DI.Setup(nameof(Composition))
                                     .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                                     .Bind().As({{lifetime}}).To(ctx =>
                                     {
                                         ctx.Inject(out Func<Logger> logger);
                                         return new Module(logger);
                                     })
                                     .Bind().As({{lifetime}}).To<Logger>()
                                     .Root<App>("Root")
                                     .Root<Logger>("LoggerRoot");
                             }

                             class Program
                             {
                                 static void Main()
                                 {
                                     var composition = new Composition();
                                     Console.WriteLine(composition.Root.Logger().Module.Name);
                                     Console.WriteLine(ReferenceEquals(composition.LoggerRoot, composition.Root.Logger()));
                                     Console.WriteLine(Module.Count);
                                 }
                             }
                             """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["Module", "True", "1"]);
        result.Success.ShouldBeTrue(result);
    }

    // The thread safety is disabled, so the local functions check the instances without locks.
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ShouldCreateInstanceInCreationWithoutThreadSafety(string lifetime, string localFunctions)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;
                             using static Pure.DI.Lifetime;

                             namespace Sample;

                             public interface IService { }
                             public sealed class Service : IService { }

                             public sealed class Module
                             {
                                 public Module(Func<Logger> logger, Func<Tracker> tracker) { }

                                 [Export]
                                 public IService Service { get; } = new Service();
                             }

                             public sealed class Logger
                             {
                                 public Logger(IService service) =>
                                     Service = service ?? throw new InvalidOperationException("Service was null");

                                 public IService Service { get; }
                             }

                             public sealed class Tracker
                             {
                                 public Tracker(Logger logger, IService service)
                                 {
                                     Logger = logger;
                                     Service = service ?? throw new InvalidOperationException("Service was null");
                                 }

                                 public Logger Logger { get; }

                                 public IService Service { get; }
                             }

                             public sealed class App
                             {
                                 private readonly Func<Tracker> _tracker;

                                 public App(Func<Module> module, Func<Tracker> tracker) => _tracker = tracker;

                                 public Tracker Tracker() => _tracker();
                             }

                             partial class Composition
                             {
                                 private static void Setup() => DI.Setup(nameof(Composition))
                                     .Hint(Hint.ThreadSafe, "Off")
                                     .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                                     .Bind().As({{lifetime}}).To<Module>()
                                     .Bind().As({{lifetime}}).To<Logger>()
                                     .Bind().As({{lifetime}}).To<Tracker>()
                                     .Root<App>("Root");
                             }

                             class Program
                             {
                                 static void Main()
                                 {
                                     var composition = new Composition();
                                     var tracker = composition.Root.Tracker();
                                     Console.WriteLine(ReferenceEquals(tracker.Service, tracker.Logger.Service));
                                 }
                             }
                             """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True"]);
        result.Success.ShouldBeTrue(result);
    }
}
