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
}
