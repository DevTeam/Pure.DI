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
                           sealed class Writes : IWrites
                           {
                               public Writes(ISignal signal) =>
                                   Signal = signal ?? throw new InvalidOperationException("Signal was null");

                               public ISignal Signal { get; }
                           }

                           interface ITool { IWrites Writes { get; } }
                           sealed class FirstTool : ITool
                           {
                               private readonly Func<IDispatcher> _dispatcher;

                               public FirstTool(Func<IDispatcher> dispatcher, IWrites writes)
                               {
                                   _dispatcher = dispatcher;
                                   Writes = writes;
                               }

                               public IWrites Writes { get; }

                               public IDispatcher Dispatcher => _dispatcher();
                           }
                           sealed class SecondTool : ITool
                           {
                               private readonly Func<IDispatcher> _dispatcher;

                               public SecondTool(Func<IDispatcher> dispatcher, IWrites writes)
                               {
                                   _dispatcher = dispatcher;
                                   Writes = writes;
                               }

                               public IWrites Writes { get; }

                               public IDispatcher Dispatcher => _dispatcher();
                           }

                           sealed class ToolHost
                           {
                               public ToolHost(System.Collections.Generic.IEnumerable<ITool> tools) =>
                                   Tools = System.Linq.Enumerable.ToArray(tools);

                               public ITool[] Tools { get; }
                           }

                           interface IConnection { ToolHost Host { get; } }
                           sealed class Connection : IConnection
                           {
                               public Connection(ToolHost host) => Host = host;

                               public ToolHost Host { get; }
                           }

                           interface ISessionFactory { ToolHost Host { get; } }
                           sealed class SessionFactory : ISessionFactory
                           {
                               public SessionFactory(System.Collections.Generic.IEnumerable<IConnection> connections) =>
                                   Host = System.Linq.Enumerable.First(connections).Host;

                               public ToolHost Host { get; }
                           }

                           interface IDispatcher { ISessionFactory Sessions { get; } }
                           sealed class Dispatcher : IDispatcher
                           {
                               private readonly Func<ISessionFactory> _sessions;

                               public Dispatcher(Func<ISessionFactory> sessions) => _sessions = sessions;

                               public ISessionFactory Sessions => _sessions();
                           }

                           interface IPublisher { }
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
