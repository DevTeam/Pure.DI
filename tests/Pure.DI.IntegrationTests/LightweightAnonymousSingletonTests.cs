namespace Pure.DI.IntegrationTests;

public class LightweightAnonymousSingletonTests
{
    [Fact]
    public async Task ShouldInitializeSharedSingletonBeforePassingItToDeferredFactoryServices()
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
                           sealed class FirstTool(Func<IDispatcher> dispatcher, IWrites writes) : ITool
                           {
                               public IWrites Writes { get; } = writes;
                               public IDispatcher Dispatcher => dispatcher();
                           }
                           sealed class SecondTool(Func<IDispatcher> dispatcher, IWrites writes) : ITool
                           {
                               public IWrites Writes { get; } = writes;
                               public IDispatcher Dispatcher => dispatcher();
                           }

                           sealed class ToolHost(System.Collections.Generic.IEnumerable<ITool> tools)
                           {
                               public ITool[] Tools { get; } = System.Linq.Enumerable.ToArray(tools);
                           }

                           interface IConnection { ToolHost Host { get; } }
                           sealed class Connection(ToolHost host) : IConnection { public ToolHost Host { get; } = host; }

                           interface ISessionFactory { ToolHost Host { get; } }
                           sealed class SessionFactory(System.Collections.Generic.IEnumerable<IConnection> connections) : ISessionFactory
                           {
                               public ToolHost Host { get; } = System.Linq.Enumerable.First(connections).Host;
                           }

                           interface IDispatcher { ISessionFactory Sessions { get; } }
                           sealed class Dispatcher(Func<ISessionFactory> sessions) : IDispatcher
                           {
                               public ISessionFactory Sessions => sessions();
                           }

                           interface IPublisher { }
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
                                   .Root<ISessionFactory>()
                                   .Root<ISignal>()
                                   .Root<IWrites>()
                                   .Singleton<Signal, Writes, ToolHost, SessionFactory, Dispatcher, Publisher>()
                                   .Singleton<FirstTool, SecondTool, Connection>(Tag.Unique);
                           }

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   // Resolve the dispatcher first: the publisher root was generated
                                   // but has never initialized the signal at runtime.
                                   var dispatcher = composition.Resolve<IDispatcher>();
                                   var sessions = dispatcher.Sessions;
                                   Console.WriteLine(ReferenceEquals(sessions.Host.Tools[0].Writes.Signal, composition.Resolve<ISignal>()));
                                   Console.WriteLine(ReferenceEquals(sessions.Host.Tools[1].Writes, composition.Resolve<IWrites>()));
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["True", "True"]);
        result.Success.ShouldBeTrue(result);
    }
}
