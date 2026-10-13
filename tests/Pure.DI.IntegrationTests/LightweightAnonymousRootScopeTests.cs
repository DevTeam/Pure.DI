namespace Pure.DI.IntegrationTests;

using Core;

public class LightweightAnonymousRootScopeTests
{
    [Fact]
    public async Task ShouldDeclareDeferredRootFactoryOutsideSiblingFactory()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IRegistry { }
                               sealed class Registry : IRegistry
                               {
                                   public Registry(IEnumerable<IPolicy> policies) { }
                               }

                               interface IPolicy { }
                               sealed class Policy : IPolicy
                               {
                                   public Policy(IService service) { }
                               }

                               interface ITool { }
                               sealed class Tool : ITool
                               {
                                   public Tool(Func<IDispatcher> dispatcher, Func<IRegistry> registry) { }
                               }

                               sealed class ToolHost
                               {
                                   public ToolHost(IEnumerable<ITool> tools) { }
                               }

                               interface IConnection { }
                               sealed class Connection : IConnection
                               {
                                   public Connection(ToolHost host) { }
                               }

                               interface ISession { }
                               sealed class Session : ISession
                               {
                                   public Session(IEnumerable<IConnection> connections) { }
                               }

                               interface IDispatcher { }
                               sealed class Dispatcher : IDispatcher
                               {
                                   public Dispatcher(Func<ISession> session) { }
                               }

                               interface IService { }
                               interface IRouter { }
                               sealed class Router : IRouter
                               {
                                   public Router(Func<IRegistry> registry) { }
                               }

                               sealed class Service : IService
                               {
                                   public Service(IRouter router, Func<IRegistry> registry) { }
                               }

                               interface IReview { }
                               sealed class Review : IReview
                               {
                                   public Review(IService service) { }
                               }

                               interface IWorkspace { }
                               sealed class Workspace : IWorkspace
                               {
                                   public Workspace(IService service, IDispatcher dispatcher) { }
                               }

                               sealed class App
                               {
                                   public App(IWorkspace workspace) { }
                               }

                               partial class BaseComposition
                               {
                                   void Setup() => DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                       .Root<IWorkspace>()
                                       .Root<IReview>()
                                       .Root<IDispatcher>()
                                       .Root<ISession>()
                                       .Root<IRegistry>()
                                       .Singleton<Registry, Workspace, Router, ToolHost, Session, Dispatcher>()
                                       .Singleton<Tool, Connection>(Tag.Unique)
                                       .Transient<Service, Review, Policy>();
                               }

                               partial class Composition
                               {
                                   void Setup() => DI.Setup(nameof(Composition))
                                       .DependsOn(nameof(BaseComposition))
                                       .Root<App>(nameof(App));
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.App is not null);
                                       Console.WriteLine(composition.Resolve<IWorkspace>() is not null);
                                       Console.WriteLine(composition.Resolve<IReview>() is not null);
                                       Console.WriteLine(composition.Resolve<IRegistry>() is not null);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Errors.ShouldBeEmpty();
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True"], result);
        result.GeneratedCode.ShouldNotContain("IRegistry = perBlockFuncIRegistry,");
    }
}
