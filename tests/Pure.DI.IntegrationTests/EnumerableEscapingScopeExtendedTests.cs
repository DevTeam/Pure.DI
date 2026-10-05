// ReSharper disable StringLiteralTypo

namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
///     Stronger reproductions of the "generated enumerable escapes its declaration scope" defect:
///     the per-block enumerable variable is needed both inside a deferred factory local function and
///     in the enclosing method, together with tagged transient implementations and a singleton root.
/// </summary>
public class EnumerableEscapingScopeExtendedTests
{
    [Fact]
    public async Task ShouldNotEscapeEnumerableNeededInsideDeferredFactoryAndOutside()
    {
        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IMcpServerConnection { string Name { get; } }
                           sealed class StdioConnection : IMcpServerConnection { public string Name => "stdio"; }
                           sealed class HttpConnection : IMcpServerConnection { public string Name => "http"; }

                           interface IAppTool { string Name { get; } }
                           sealed class ReadTool : IAppTool { public string Name => "read"; }
                           sealed class WriteTool : IAppTool { public string Name => "write"; }

                           interface IToolSession { string Info { get; } }

                           sealed class ToolSession : IToolSession
                           {
                               private readonly IEnumerable<IMcpServerConnection> _connections;
                               private readonly IEnumerable<IAppTool> _tools;

                               public ToolSession(
                                   IEnumerable<IMcpServerConnection> connections,
                                   IEnumerable<IAppTool> tools)
                               {
                                   _connections = connections;
                                   _tools = tools;
                               }

                               public string Info =>
                                   string.Join(",", _connections.Select(i => i.Name)) + "|" +
                                   string.Join(",", _tools.Select(i => i.Name));
                           }

                           interface IToolSessionFactory { IToolSession Create(); }

                           sealed class ToolSessionFactory : IToolSessionFactory
                           {
                               private readonly Func<IToolSession> _factory;

                               public ToolSessionFactory(Func<IToolSession> factory) => _factory = factory;

                               public IToolSession Create() => _factory();
                           }

                           sealed class Host
                           {
                               private readonly IToolSessionFactory _factory;
                               private readonly Func<IToolSession> _deferred;

                               public Host(
                                   IToolSessionFactory factory,
                                   Func<IToolSession> deferred)
                               {
                                   _factory = factory;
                                   _deferred = deferred;
                               }

                               public string Run() => _factory.Create().Info + ";" + _deferred().Info;
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IMcpServerConnection>(Tag.Unique).To<StdioConnection>()
                                   .Bind<IMcpServerConnection>(Tag.Unique).To<HttpConnection>()
                                   .Bind<IAppTool>(Tag.Unique).To<ReadTool>()
                                   .Bind<IAppTool>(Tag.Unique).To<WriteTool>()
                                   .Bind<IToolSession>().To<ToolSession>()
                                   .Bind<IToolSessionFactory>().To<ToolSessionFactory>()
                                   .Bind<Host>().As(Lifetime.Singleton).To<Host>()
                                   .Root<Host>("Host")
                                   .Root<Func<IToolSession>>("SessionFactory");
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Host.Run());
                                   Console.WriteLine(composition.SessionFactory().Info);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Errors.ShouldBeEmpty();
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["stdio,http|read,write;stdio,http|read,write", "stdio,http|read,write"], result);
    }

    [Fact]
    public async Task ShouldNotEscapeEnumerableWithExplicitPerBlockLifetime()
    {
        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IConnection { string Name { get; } }
                           sealed class TcpConnection : IConnection { public string Name => "tcp"; }
                           sealed class UdpConnection : IConnection { public string Name => "udp"; }

                           interface IConnectionSet { string Names { get; } }

                           sealed class ConnectionSet : IConnectionSet
                           {
                               private readonly IEnumerable<IConnection> _connections;

                               public ConnectionSet(IEnumerable<IConnection> connections) =>
                                   _connections = connections;

                               public string Names => string.Join(",", _connections.Select(i => i.Name));
                           }

                           sealed class Consumer
                           {
                               private readonly IConnectionSet _set;
                               private readonly Func<IConnectionSet> _deferred;

                               public Consumer(
                                   IConnectionSet set,
                                   Func<IConnectionSet> deferred)
                               {
                                   _set = set;
                                   _deferred = deferred;
                               }

                               public string Run() => _set.Names + ";" + _deferred().Names;
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IConnection>(Tag.Unique).As(Lifetime.PerBlock).To<TcpConnection>()
                                   .Bind<IConnection>(Tag.Unique).As(Lifetime.PerBlock).To<UdpConnection>()
                                   .Bind<IConnectionSet>().To<ConnectionSet>()
                                   .Bind<Consumer>().To<Consumer>()
                                   .Root<Consumer>("Consumer");
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Consumer.Run());
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Errors.ShouldBeEmpty();
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["tcp,udp;tcp,udp"], result);
    }
}
