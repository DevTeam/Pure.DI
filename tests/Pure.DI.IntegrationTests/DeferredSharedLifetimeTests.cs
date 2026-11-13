namespace Pure.DI.IntegrationTests;

public class DeferredSharedLifetimeTests
{
    [Fact]
    public async Task ShouldInitializeSingletonWhenADeferredFactoryRunsBeforeAnEarlierRoot()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface ISignal { }
                           sealed class Signal : ISignal { }

                           interface IWrites { ISignal Signal { get; } }
                           sealed class Writes : IWrites
                           {
                               public Writes(ISignal signal) =>
                                   Signal = signal ?? throw new InvalidOperationException("Signal was null");

                               public ISignal Signal { get; }
                           }

                           interface ITool { IWrites Writes { get; } }
                           sealed class FirstTool : ITool
                           {
                               public FirstTool(IWrites writes) => Writes = writes;

                               public IWrites Writes { get; }
                           }
                           sealed class SecondTool : ITool
                           {
                               public SecondTool(IWrites writes) => Writes = writes;

                               public IWrites Writes { get; }
                           }

                           interface ISession { ITool[] Tools { get; } }
                           sealed class Session : ISession
                           {
                               public Session(System.Collections.Generic.IEnumerable<ITool> tools) =>
                                   Tools = System.Linq.Enumerable.ToArray(tools);

                               public ITool[] Tools { get; }
                           }

                           interface IDispatcher { ISession Open(); }
                           sealed class Dispatcher : IDispatcher
                           {
                               private readonly Func<ISession> _sessions;

                               public Dispatcher(Func<ISession> sessions) => _sessions = sessions;

                               public ISession Open() => _sessions();
                           }

                           interface IPublisher { ISignal Signal { get; } }
                           sealed class Publisher : IPublisher
                           {
                               public Publisher(ISignal signal, IDispatcher dispatcher)
                               {
                                   Signal = signal;
                                   Dispatcher = dispatcher;
                               }

                               public ISignal Signal { get; }

                               public IDispatcher Dispatcher { get; }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Root<IPublisher>()
                                   .Root<IDispatcher>()
                                   .Root<ISignal>()
                                   .Root<IWrites>()
                                   .Singleton<Signal, Writes, Session, Dispatcher, Publisher>()
                                   .Singleton<FirstTool, SecondTool>(Tag.Unique);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   var session = composition.Resolve<IDispatcher>().Open();
                                   Console.WriteLine(session.Tools.Length == 2);
                                   Console.WriteLine(ReferenceEquals(session.Tools[0].Writes.Signal, composition.Resolve<IPublisher>().Signal));
                                   Console.WriteLine(ReferenceEquals(session.Tools[1].Writes, composition.Resolve<IWrites>()));
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True", "True", "True"]);
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldInitializeScopedDependencyForDeferredEnumerable()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface ISignal { }
                           sealed class Signal : ISignal { }

                           interface ITool { ISignal Signal { get; } }
                           sealed class FirstTool : ITool
                           {
                               public FirstTool(ISignal signal) =>
                                   Signal = signal ?? throw new InvalidOperationException("Signal was null");

                               public ISignal Signal { get; }
                           }
                           sealed class SecondTool : ITool
                           {
                               public SecondTool(ISignal signal) =>
                                   Signal = signal ?? throw new InvalidOperationException("Signal was null");

                               public ISignal Signal { get; }
                           }

                           interface IDispatcher { ITool[] Open(); }
                           sealed class Dispatcher : IDispatcher
                           {
                               private readonly Func<IEnumerable<ITool>> _tools;

                               public Dispatcher(Func<IEnumerable<ITool>> tools) => _tools = tools;

                               public ITool[] Open() => _tools().ToArray();
                           }

                           interface IPublisher { ISignal Signal { get; } }
                           sealed class Publisher : IPublisher
                           {
                               public Publisher(ISignal signal, IDispatcher dispatcher)
                               {
                                   Signal = signal;
                                   Dispatcher = dispatcher;
                               }

                               public ISignal Signal { get; }

                               public IDispatcher Dispatcher { get; }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Root<IPublisher>()
                                   .Root<IDispatcher>()
                                   .Root<ISignal>()
                                   .Bind<ISignal>().As(Lifetime.Scoped).To<Signal>()
                                   .Singleton<Dispatcher, Publisher>()
                                   .Singleton<FirstTool, SecondTool>(Tag.Unique);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   var tools = composition.Resolve<IDispatcher>().Open();
                                   var signal = composition.Resolve<IPublisher>().Signal;
                                   Console.WriteLine(tools.Length == 2);
                                   Console.WriteLine(ReferenceEquals(tools[0].Signal, signal));
                                   Console.WriteLine(ReferenceEquals(tools[1].Signal, signal));
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True", "True", "True"]);
        result.Success.ShouldBeTrue(result);
    }

    // #202 The local function that creates a singleton is first generated under the creation of another singleton,
    // which it reaches through a lambda, but it is also called by code that runs before that singleton exists.
    [Fact]
    public async Task ShouldCreateExportingSingletonWhenTheSingletonThatUsesItIsCreatedFirst()
    {
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample;

                           public interface IService { }
                           public sealed class Service : IService { }

                           public sealed class Module
                           {
                               public Module(Func<Logger> logger) { }

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
                               private readonly Func<Logger> _logger;

                               public App(Func<Module> module, Func<Logger> logger) => _logger = logger;

                               public Logger Logger() => _logger();
                           }

                           partial class Composition
                           {
                               private static void Setup() => DI.Setup(nameof(Composition))
                                   .Bind().As(Singleton).To<Module>()
                                   .Bind().As(Singleton).To<Logger>()
                                   .Root<App>("Root");
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Root.Logger().Service);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["Sample.Service"]);
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldCreateExportingSingletonWhenSeveralLambdasOfItLeadToSingletonsThatUseIt()
    {
        var result = await """
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
                               public Tracker(IService service) =>
                                   Service = service ?? throw new InvalidOperationException("Service was null");

                               public IService Service { get; }
                           }

                           public sealed class App
                           {
                               private readonly Func<Module> _module;
                               private readonly Func<Logger> _logger;
                               private readonly Func<Tracker> _tracker;

                               public App(Func<Module> module, Func<Logger> logger, Func<Tracker> tracker)
                               {
                                   _module = module;
                                   _logger = logger;
                                   _tracker = tracker;
                               }

                               public Module Module() => _module();

                               public Logger Logger() => _logger();

                               public Tracker Tracker() => _tracker();
                           }

                           partial class Composition
                           {
                               private static void Setup() => DI.Setup(nameof(Composition))
                                   .Bind().As(Singleton).To<Module>()
                                   .Bind().As(Singleton).To<Logger>()
                                   .Bind().As(Singleton).To<Tracker>()
                                   .Root<App>("Root");
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   var app = composition.Root;
                                   var tracker = app.Tracker();
                                   var logger = app.Logger();
                                   Console.WriteLine(ReferenceEquals(tracker.Service, logger.Service));
                                   Console.WriteLine(ReferenceEquals(app.Module().Service, logger.Service));
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True", "True"]);
        result.Success.ShouldBeTrue(result);
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("On")]
    public async Task ShouldCreateSingletonInjectedIntoTransientWhenTheTransientIsFirstCreatedUnderIt(string localFunctions)
    {
        var result = await $$"""
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample;

                           public sealed class Settings { }

                           public sealed class Host
                           {
                               public Host(Settings settings, Func<Worker> worker) => Settings = settings;

                               public Settings Settings { get; }
                           }

                           public sealed class Worker
                           {
                               private readonly Func<Worker> _next;

                               public Worker(Host host, Func<Worker> next)
                               {
                                   Host = host ?? throw new InvalidOperationException("Host was null");
                                   _next = next;
                               }

                               public Host Host { get; }

                               public Worker Next() => _next();
                           }

                           public sealed class App
                           {
                               private readonly Func<Worker> _worker;

                               public App(Func<Host> host, Func<Worker> worker) => _worker = worker;

                               public Worker Worker() => _worker();
                           }

                           partial class Composition
                           {
                               private static void Setup() => DI.Setup(nameof(Composition))
                                   .Bind().As(Singleton).To<Settings>()
                                   .Bind().As(Singleton).To<Host>()
                                   .Bind().To<Worker>()
                                   .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                                   .Root<App>("Root");
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   var app = composition.Root;
                                   var first = app.Worker();
                                   var second = app.Worker().Next();
                                   Console.WriteLine(ReferenceEquals(first.Host, second.Host));
                                   Console.WriteLine(first.Host.Settings is not null);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True", "True"]);
        result.Success.ShouldBeTrue(result);
    }
}
