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
                           sealed class Writes(ISignal signal) : IWrites
                           {
                               public ISignal Signal { get; } = signal ?? throw new InvalidOperationException("Signal was null");
                           }

                           interface ITool { IWrites Writes { get; } }
                           sealed class FirstTool(IWrites writes) : ITool { public IWrites Writes { get; } = writes; }
                           sealed class SecondTool(IWrites writes) : ITool { public IWrites Writes { get; } = writes; }

                           interface ISession { ITool[] Tools { get; } }
                           sealed class Session(System.Collections.Generic.IEnumerable<ITool> tools) : ISession
                           {
                               public ITool[] Tools { get; } = System.Linq.Enumerable.ToArray(tools);
                           }

                           interface IDispatcher { ISession Open(); }
                           sealed class Dispatcher(Func<ISession> sessions) : IDispatcher
                           {
                               public ISession Open() => sessions();
                           }

                           interface IPublisher { ISignal Signal { get; } }
                           sealed class Publisher(ISignal signal, IDispatcher dispatcher) : IPublisher
                           {
                               public ISignal Signal { get; } = signal;
                               public IDispatcher Dispatcher { get; } = dispatcher;
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
                           sealed class FirstTool(ISignal signal) : ITool
                           {
                               public ISignal Signal { get; } = signal ?? throw new InvalidOperationException("Signal was null");
                           }
                           sealed class SecondTool(ISignal signal) : ITool
                           {
                               public ISignal Signal { get; } = signal ?? throw new InvalidOperationException("Signal was null");
                           }

                           interface IDispatcher { ITool[] Open(); }
                           sealed class Dispatcher(Func<IEnumerable<ITool>> tools) : IDispatcher
                           {
                               public ITool[] Open() => tools().ToArray();
                           }

                           interface IPublisher { ISignal Signal { get; } }
                           sealed class Publisher(ISignal signal, IDispatcher dispatcher) : IPublisher
                           {
                               public ISignal Signal { get; } = signal;
                               public IDispatcher Dispatcher { get; } = dispatcher;
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
