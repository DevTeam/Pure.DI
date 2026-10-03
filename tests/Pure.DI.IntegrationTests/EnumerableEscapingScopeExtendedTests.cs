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

                           sealed class ToolSession(
                               IEnumerable<IMcpServerConnection> connections,
                               IEnumerable<IAppTool> tools) : IToolSession
                           {
                               public string Info =>
                                   string.Join(",", connections.Select(i => i.Name)) + "|" +
                                   string.Join(",", tools.Select(i => i.Name));
                           }

                           interface IToolSessionFactory { IToolSession Create(); }

                           sealed class ToolSessionFactory(Func<IToolSession> factory) : IToolSessionFactory
                           {
                               public IToolSession Create() => factory();
                           }

                           sealed class Host(
                               IToolSessionFactory factory,
                               Func<IToolSession> deferred)
                           {
                               public string Run() => factory.Create().Info + ";" + deferred().Info;
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

                           sealed class ConnectionSet(IEnumerable<IConnection> connections) : IConnectionSet
                           {
                               public string Names => string.Join(",", connections.Select(i => i.Name));
                           }

                           sealed class Consumer(
                               IConnectionSet set,
                               Func<IConnectionSet> deferred)
                           {
                               public string Run() => set.Names + ";" + deferred().Names;
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
