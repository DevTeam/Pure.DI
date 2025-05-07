﻿namespace Pure.DI.IntegrationTests;

using Core;

public class FuncTests
{
    [Theory]
    [InlineData(LanguageVersion.CSharp8)]
    [InlineData(LanguageVersion.CSharp9)]
    [InlineData(LanguageVersion.CSharp10)]
    public async Task ShouldSupportFuncForTransientDependencies(LanguageVersion languageVersion)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<IDependency> _depFactory;
                                   public Service(Func<IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep != service.Dep);
                                   }
                               }
                           }
                           """.RunAsync(new Options(languageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportComplexFuncOfEnumerable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Collections.Generic;

                           namespace Sample
                           {
                               class ServiceApplication
                               {
                                   public ServiceApplication(System.Func<bool, ServiceHostEnvironment> factory)
                                   {
                                       factory(true);
                                   }
                               }
                           
                               interface IProcessRegistry
                               {
                               }
                           
                               class ProcessRegistry: IProcessRegistry
                               {
                               }
                           
                               class ServiceHostEnvironment
                               {
                                   public ServiceHostEnvironment(
                                       Func<ILogger2> loggerFactory,
                                       Func<IServiceHost> serviceHostFactory,
                                       bool registerForRestart)
                                   {
                                       serviceHostFactory();
                                   }
                               }
                           
                               interface ILogger2: IDisposable
                               {
                               }
                           
                               class Logger2: ILogger2
                               {
                                   public void Dispose()
                                   {
                                   }
                               }
                           
                               class LoggerConfiguration
                               {
                                   public ILogger2 CreateLogger()
                                   {
                                       return new Logger2();
                                   }
                               }
                           
                               interface IServiceHost
                               {
                               }
                           
                               interface IConfiguration
                               {
                                   string this[string key] { get; }
                                   
                                   void Init();
                               }
                           
                               class AppConfiguration : IConfiguration
                               {
                                   public string this[string key] => "";
                           
                                   void IConfiguration.Init()
                                   {
                                   }
                               }
                           
                               interface ITelemetry
                               {
                               }
                           
                               class SentryIOTelemetry : ITelemetry
                               {
                                   public SentryIOTelemetry()
                                   {
                                   }
                               }
                           
                               class TelemetryRuntime: IDisposable
                               {
                                   public TelemetryRuntime(TelemetryRuntimeParameters parameters)
                                   {
                                   }
                           
                                   public void Dispose()
                                   {
                                   }
                               }
                           
                               interface IDeploymentContext
                               {
                                   object GetDeploymentTypeName();
                               }
                           
                               class DeploymentContext: IDeploymentContext
                               {
                                   public object GetDeploymentTypeName()
                                   {
                                       return new object();
                                   }
                               }
                           
                               class TelemetryRuntimeParameters
                               {
                                   public TelemetryRuntimeParameters(
                                       string mobisystemsServicehost,
                                       string s,
                                       string s1,
                                       object getDeploymentTypeName,
                                       Func<object, bool> func)
                                   {
                                   }
                               }
                           
                               interface IService
                               {
                               }
                           
                               class PackageUpdaterService: IService
                               {
                                   public PackageUpdaterService(ITelemetry telemetry, ILogger2 logger2, IConfiguration configuration)
                                   {
                                   }
                               }
                           
                               class ServiceHost: IServiceHost
                               {
                                   public ServiceHost(
                                       ITelemetry telemetry, System.Func<System.Collections.Generic.IEnumerable<IService>> services, IProcessRegistry processRegistry, ILogger2 logger2)
                                   {
                                       foreach (var service in services())
                                       {
                                       }
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Root<ServiceApplication>("Root")
                                           .Bind<IEnumerable<TT>>().To(ctx =>
                                           {
                                               ctx.Inject(out IReadOnlyCollection<TT> items);
                                               return items;
                                           })
                                           .Bind<IProcessRegistry>().As(Lifetime.Singleton).To(ctx => new ProcessRegistry())
                                           .Bind<Func<bool, ServiceHostEnvironment>>().To<Func<bool, ServiceHostEnvironment>>(ctx => (bool registerForRestart) =>
                                           {
                                               ctx.Inject(out Func<ILogger2> loggerFactory);
                                               ctx.Inject(out Func<IServiceHost> serviceHostFactory);
                                               return new ServiceHostEnvironment(loggerFactory, serviceHostFactory, registerForRestart);
                                           })
                                           .Bind<IConfiguration>().As(Lifetime.Singleton).To(ctx => 
                                           {
                                               var configuration = new AppConfiguration();
                                               ((IConfiguration)configuration).Init();
                                               return configuration;
                                           })
                                           .Bind<ITelemetry>().As(Lifetime.Singleton).To(ctx => 
                                           {
                                               //SentryIOTelemetry requires TelemetryRuntime to be initialized first
                                               ctx.Inject(out TelemetryRuntime telemetryRuntime);
                                               return new SentryIOTelemetry();
                                           })
                                           .Bind().As(Lifetime.Singleton).To<TelemetryRuntime>(ctx => 
                                           {
                                               ctx.Inject(out IDeploymentContext deploymentContext);
                                               ctx.Inject(out IConfiguration configuration);
                           
                                               var parameters = new TelemetryRuntimeParameters(
                                                   "MobiSystems.ServiceHost",
                                                   configuration["TelemetryHost"],
                                                   configuration["BuildType"],
                                                   deploymentContext.GetDeploymentTypeName(),
                                                   (_) => true);
                           
                                               return new TelemetryRuntime(parameters);
                                           })
                                           .Bind<IDeploymentContext>().As(Lifetime.Singleton).To<DeploymentContext>()
                                           .Bind<IService>(1).To<PackageUpdaterService>()
                                           .Bind<ILogger2>().As(Lifetime.Singleton).To(ctx =>
                                           {
                                               var logger = new LoggerConfiguration()
                                                   .CreateLogger();
                                               
                                               return logger;
                                           })
                                           .Bind().As(Lifetime.Singleton).To<ServiceHost>();
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var app = new Composition().Root;
                                       Console.WriteLine(app);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.ServiceApplication"], result);
    }

    [Fact]
    public async Task ShouldSupportFuncForPerResolveDependencies()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<IDependency> _depFactory;
                           
                                   public Service(Func<IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory();
                               }
                               
                               class Consumer 
                               {
                                   private Func<IService> _serviceFactory;
                                   private Func<IDependency> _depFactory;
                           
                                   public Consumer(Func<IService> serviceFactory, Func<IDependency> depFactory)
                                   { 
                                       _serviceFactory = serviceFactory;
                                       _depFactory = depFactory;
                                   }
                           
                                   public IService Service => _serviceFactory();
                                   public IDependency Dep => _depFactory();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<Consumer>("Consumer");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var consumer = composition.Consumer;
                                       var service = consumer.Service;
                                       Console.WriteLine(service.Dep == service.Dep);
                                       Console.WriteLine(consumer.Dep == service.Dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportFuncForSingletonDependencies()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<IDependency> _depFactory;
                                   public Service(Func<IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep == service.Dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFuncOfArray()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Linq;
                           using System.Collections.Generic;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private IDependency[] _depFactory;
                                   public Service(Func<Func<IDependency[]>> depFactory)
                                   { 
                                       _depFactory = depFactory()();
                                   }
                           
                                   public IDependency Dep => _depFactory[0];
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportFuncOfFuncOfFunc()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<IDependency> _depFactory;
                                   public Service(Func<Func<Func<IDependency>>> depFactory)
                                   { 
                                       _depFactory = depFactory()();
                                   }
                           
                                   public IDependency Dep => _depFactory();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep != service.Dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFuncOfIEnumerable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Linq;
                           using System.Collections.Generic;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private System.Collections.Generic.IEnumerable<IDependency> _depFactory;
                                   public Service(Func<Func<System.Collections.Generic.IEnumerable<IDependency>>> depFactory)
                                   { 
                                       _depFactory = depFactory()();
                                   }
                           
                                   public IDependency Dep => _depFactory.First();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep != service.Dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFuncOfTuple()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Linq;
                           using System.Collections.Generic;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private System.Collections.Generic.IEnumerable<IDependency> _depFactory;
                                   public Service(Func<(System.Collections.Generic.IEnumerable<IDependency>, System.Collections.Generic.IEnumerable<IDependency>)> depFactory)
                                   { 
                                       _depFactory = depFactory().Item1;
                                   }
                           
                                   public IDependency Dep => _depFactory.First();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep != service.Dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFuncWithArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                           
                                   public Dependency(string name)
                                   {
                                       _name = name;
                                   }
                           
                                   public override string ToString() => _name;
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, IDependency> _depFactory;
                                   public Service([Tag(typeof(string))] Func<string, IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz");
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<System.Func<string, IDependency>>(typeof(string)).To(ctx => new System.Func<string, IDependency>(i => 
                                           {
                                               ctx.Inject<string>(out var prefix);
                                               return new Dependency(prefix + i);
                                           }))
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("prefix")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Abc");
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["AbcXyz"], result);
    }

    [Fact]
    public async Task ShouldSupportFuncWithCodeStatement()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                           
                                   public Dependency(string name)
                                   {
                                       _name = name;
                                   }
                           
                                   public override string ToString() => _name;
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, IDependency> _depFactory;
                                   public Service([Tag(typeof(string))] Func<string, IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz");
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>().To<string>("strArg")
                                           .Bind<System.Func<string, IDependency>>(typeof(string)).To(ctx => new System.Func<string, IDependency>(strArg => 
                                           {
                                               ctx.Inject<Dependency>(out var dep);
                                               return dep;
                                           }))
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Xyz"], result);
    }

    [Fact]
    public async Task ShouldSupportFuncWithCodeStatementAndInferredDelegateType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                           
                                   public Dependency(string name)
                                   {
                                       _name = name;
                                   }
                           
                                   public override string ToString() => _name;
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, IDependency> _depFactory;
                                   public Service([Tag(typeof(string))] Func<string, IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz");
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>().To<string>("strArg")
                                           .Bind<System.Func<string, IDependency>>(typeof(string)).To<System.Func<string, IDependency>>(ctx => strArg => 
                                           {
                                               ctx.Inject<Dependency>(out var dep);
                                               return dep;
                                           })
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Xyz"], result);
    }

    [Fact]
    public async Task ShouldSupportFuncWithTag()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<IDependency> _depFactory;
                                   public Service([Tag("Abc")] Func<IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory();
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>("Abc").To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep != service.Dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportStdFuncWithArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                           
                                   public Dependency(string name)
                                   {
                                       _name = name;
                                   }
                           
                                   public override string ToString() => _name;
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, IDependency> _depFactory;
                                   public Service(Func<string, IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz");
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Xyz"], result);
    }

    [Fact]
    public async Task ShouldSupportStdFuncWithDifferentArgs()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                           
                                   public Dependency(string name)
                                   {
                                       _name = name;
                                   }
                           
                                   public override string ToString() => _name;
                               }
                               
                               interface IDependency2 {}
                           
                               class Dependency2: IDependency2
                               {
                                   private int _id;
                               
                                   public Dependency2(int id)
                                   {
                                       _id = id;
                                   }
                               
                                   public override string ToString() => _id.ToString();
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, IDependency> _depFactory;
                                   public Service(
                                        Func<string, IDependency> depFactory,
                                        Func<int, IDependency2> depFactory2)
                                   { 
                                       _depFactory = depFactory;
                                       depFactory2(33);
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz");
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Dependency2>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Xyz"], result);
    }

    [Fact]
    public async Task ShouldSupportStdFuncWithDifferentNestedArgs()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               
                               class Dep: IDep
                               {
                                   private char _sym;
                               
                                   public Dep(char sym)
                                   {
                                       _sym = sym;
                                   }
                               }
                           
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                           
                                   public Dependency(string name, Func<char, IDep> depFactory)
                                   {
                                       _name = name;
                                       depFactory('a');
                                   }
                           
                                   public override string ToString() => _name;
                               }
                               
                               interface IDependency2 {}
                           
                               class Dependency2: IDependency2
                               {
                                   private int _id;
                               
                                   public Dependency2(int id)
                                   {
                                       _id = id;
                                   }
                               
                                   public override string ToString() => _id.ToString();
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, IDependency> _depFactory;
                                   public Service(
                                        Func<string, IDependency> depFactory,
                                        Func<int, IDependency2> depFactory2)
                                   { 
                                       _depFactory = depFactory;
                                       depFactory2(33);
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz");
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dep>()
                                           .Bind().To<Dependency>()
                                           .Bind().To<Dependency2>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Xyz"], result);
    }

    [Fact]
    public async Task ShouldSupportStdFuncWith2Args()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                                   private int _id;
                           
                                   public Dependency(string name, int id)
                                   {
                                       _name = name;
                                       _id = id;
                                   }
                           
                                   public override string ToString() => $"{_name} {_id}";
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, int, IDependency> _depFactory;
                                   public Service(Func<string, int, IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz", 33);
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Xyz 33"], result);
    }

    [Fact]
    public async Task ShouldSupportStdFuncWithComplexArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               
                               interface IInfo {}
                               
                               class Info: IInfo {}
                               
                               interface IContext {}
                               
                               class Context: IContext
                               {
                                    public Context(IInfo Info) {}
                               }
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                                   private IContext _ctx;
                           
                                   public Dependency(string name, IContext ctx)
                                   {
                                       _name = name;
                                       _ctx = ctx;
                                   }
                           
                                   public override string ToString() => $"{_name} {_ctx}";
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, IContext, IDependency> _depFactory;

                                   public Service(Func<string, IContext, IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz", new Context(new Info()));
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Xyz Sample.Context"], result);
    }

    [Fact]
    public async Task ShouldSupportStdFuncWithComplexGenericArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               
                               interface IInfo {}
                               
                               class Info: IInfo {}
                               
                               interface IContext<T> {}
                               
                               class Context<T>: IContext<T>
                               {
                                    public Context(IInfo Info) {}
                               }
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                                   private IContext<int> _ctx;
                           
                                   public Dependency(string name, IContext<int> ctx)
                                   {
                                       _name = name;
                                       _ctx = ctx;
                                   }
                           
                                   public override string ToString() => $"{_name} {_ctx}";
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, IContext<int>, IDependency> _depFactory;

                                   public Service(Func<string, IContext<int>, IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz", new Context<int>(new Info()));
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Xyz Sample.Context`1[System.Int32]"], result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(20)]
    [InlineData(1000)]
    public async Task ShouldSupportBuildUpWhenStdFuncWithArg(int localFunctionLines)
    {
        // Given

        // When
        var result = await """
                           #pragma warning disable CS8602
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                                    public LoggerA(string name)
                                    {
                                        Console.WriteLine(name);
                                    }
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(Func<string, ILogger> loggerFactory, int id)
                                   {
                                        Console.WriteLine(id);
                                        Logger = loggerFactory("Qwerty");
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                                   
                                   ILogger? Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   [Dependency]
                                   public Func<int, string, IDependency>? DepFactory { get; set; }
                                   
                                   [Dependency]
                                   public Func<string, ILogger>? LoggerFactory { get; set; }
                                   
                                   public IDependency? Dep => DepFactory!.Invoke(33, "Xyz");
                           
                                   public ILogger? Logger => LoggerFactory!.Invoke("Abc");
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.LocalFunctionLines, "#localFunctionLines#")
                                           .Bind().To<LoggerA>()
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                var service = new ServiceA();
                                                ctx.BuildUp(service);
                                                return service;
                                           })
                                           .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root.Dep);
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.Replace("#localFunctionLines#", localFunctionLines.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["33", "Qwerty", "Sample.Dependency", "Abc", "Sample.LoggerA", "33", "Qwerty", "Sample.LoggerA"], result);
    }

    [Fact]
    public async Task ShouldSupportBuilderWhenStdFuncWithArg()
    {
        // Given

        // When
        var result = await """
                           #pragma warning disable CS8602
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                                    public LoggerA(string name)
                                    {
                                        Console.WriteLine(name);
                                    }
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(Func<string, ILogger> loggerFactory, int id)
                                   {
                                        Console.WriteLine(id);
                                        Logger = loggerFactory("Qwerty");
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                                   
                                   ILogger? Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   [Dependency]
                                   public Func<int, string, IDependency>? DepFactory { get; set; }
                                   
                                   [Dependency]
                                   public Func<string, ILogger>? LoggerFactory { get; set; }
                                   
                                   public IDependency? Dep => DepFactory!.Invoke(33, "Xyz");
                           
                                   public ILogger? Logger => LoggerFactory!.Invoke("Abc");
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<LoggerA>()
                                           .Bind().To<Dependency>()
                                           .Builder<ServiceA>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.BuildUp(new ServiceA());
                                       Console.WriteLine(root.Dep);
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["33", "Qwerty", "Sample.Dependency", "Abc", "Sample.LoggerA", "33", "Qwerty", "Sample.LoggerA"], result);
    }

    [Fact]
    public async Task ShouldShowWarningWhenStdFuncOverrideClassArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               
                               interface IInfo {}
                               
                               class Info: IInfo {}
                               
                               interface IContext<T> {}
                               
                               class Context<T>: IContext<T>
                               {
                                    public Context(IInfo Info) {}
                               }
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                                   private IContext<int> _ctx;
                           
                                   public Dependency(string name, IContext<int> ctx)
                                   {
                                       _name = name;
                                       _ctx = ctx;
                                   }
                           
                                   public override string ToString() => $"{_name} {_ctx}";
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, IContext<int>, IDependency> _depFactory;

                                   public Service(Func<string, IContext<int>, IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz", new Context<int>(new Info()));
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Arg<string>("str")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningMetadataDefect && i.Locations.FirstOrDefault().GetSource() == "Arg<string>(\"str\")").ShouldBe(1, result);
        result.StdOut.ShouldBe(["Xyz Sample.Context`1[System.Int32]"], result);
    }

    [Fact]
    public async Task ShouldShowWarningWhenStdFuncOverrideRootArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               
                               interface IInfo {}
                               
                               class Info: IInfo {}
                               
                               interface IContext<T> {}
                               
                               class Context<T>: IContext<T>
                               {
                                    public Context(IInfo Info) {}
                               }
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                                   private IContext<int> _ctx;
                           
                                   public Dependency(string name, IContext<int> ctx)
                                   {
                                       _name = name;
                                       _ctx = ctx;
                                   }
                           
                                   public override string ToString() => $"{_name} {_ctx}";
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private Func<string, IContext<int>, IDependency> _depFactory;

                                   public Service(Func<string, IContext<int>, IDependency> depFactory)
                                   { 
                                       _depFactory = depFactory;
                                   }
                           
                                   public IDependency Dep => _depFactory("Xyz", new Context<int>(new Info()));
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .RootArg<string>("str")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep.ToString());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningMetadataDefect && i.Locations.FirstOrDefault().GetSource() == "RootArg<string>(\"str\")").ShouldBe(1, result);
        result.StdOut.ShouldBe(["Xyz Sample.Context`1[System.Int32]"], result);
    }

    [Fact]
    public async Task ShouldSupportRootToResolveWhenStdFuncWith2Args()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   private string _name;
                                   private int _id;
                           
                                   public Dependency(string name, int id)
                                   {
                                       _name = name;
                                       _id = id;
                                   }
                           
                                   public override string ToString() => $"{_name} {_id}";
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Root<Func<string, int, IDependency>>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var dep = composition.Resolve<Func<string, int, IDependency>>()("Asd", 55);
                                       Console.WriteLine(dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Asd 55"], result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldSupportFunWithArgWhenOverrideValueIsNotUsed()
    {
        // Given

        // When

        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.ComponentModel;
                           using System.Diagnostics;
                           using System.Runtime.CompilerServices;
                           using System.Threading;
                           using Pure.DI;

                           namespace Sample
                           {
                               public interface IClock
                               {
                                   DateTimeOffset Now { get; }
                               }
                               
                               public class Clock : IClock
                               {
                                   public DateTimeOffset Now => DateTimeOffset.Now;
                               }
                               
                               public interface IDispatcher
                               {
                                   void Dispatch(Action action);
                               }
                               
                               public class Dispatcher : IDispatcher
                               {
                                   public void Dispatch(Action action) => action();
                               }
                               
                               public interface ILog<T>
                               {
                                   void Info(string message);
                               }
                               
                               public class Log<T> : ILog<T>
                               {
                                   private readonly IClock _clock;
                               
                                   public Log(IClock clock)
                                   {
                                       _clock = clock;
                                   }
                               
                                   public void Info(string message) =>
                                       Debug.WriteLine($"{_clock.Now:HH:mm:ss.fff} {typeof(T).Name,-32} {message}");
                               }
                               
                               public interface ISubject<T> : IObservable<T>, IObserver<T>
                               {
                               }
                               
                               public class Subject<T> : ISubject<T>, IObservable<T>, IObserver<T>
                               {
                                   private readonly List<IObserver<T>> _observers = new();
                                   private readonly ILog<Subject<T>> _log;
                               
                                   public Subject(ILog<Subject<T>> log)
                                   {
                                       _log = log;
                                   }
                               
                                   public IDisposable Subscribe(IObserver<T> observer)
                                   {
                                       lock (_observers)
                                       {
                                           _observers.Add(observer);
                                           var subscription = new Subscription(_observers, observer);
                                           _log.Info("Subscribed");
                                           return subscription;
                                       }
                                   }
                               
                                   public void OnNext(T value)
                                   {
                                       _log.Info($"OnNext {value}");
                                       List<IObserver<T>> observers;
                                       lock (_observers)
                                       {
                                           observers = new List<IObserver<T>>(_observers);
                                       }
                               
                                       foreach (var observer in observers)
                                       {
                                           observer.OnNext(value);
                                       }
                                   }
                               
                                   public void OnCompleted()
                                   {
                                       _log.Info("OnCompleted");
                                       Clear();
                                   }
                               
                                   public void OnError(Exception error)
                                   {
                                       _log.Info("OnError");
                                       Clear();
                                   }
                               
                                   private void Clear()
                                   {
                                       lock (_observers)
                                       {
                                           _observers.Clear();
                                       }
                                   }
                               
                                   private class Subscription : IDisposable
                                   {
                                       private readonly ICollection<IObserver<T>> _observers1;
                                       private readonly IObserver<T> _observer;
                               
                                       public Subscription(ICollection<IObserver<T>> observers,
                                           IObserver<T> observer)
                                       {
                                           _observers1 = observers;
                                           _observer = observer;
                                       }
                               
                                       public void Dispose()
                                       {
                                           lock (_observers1)
                                           {
                                               _observers1.Remove(_observer);
                                           }
                                       }
                                   }
                               }
                               
                               public readonly record struct Tick;
                               
                               public class Metronome : IObservable<Tick>, IDisposable
                               {
                                   private readonly ILog<Metronome> _log;
                                   private readonly ISubject<Tick> _subject;
                                   private readonly Timer _timer;
                               
                                   public Metronome(
                                       ILog<Metronome> log,
                                       ISubject<Tick> subject,
                                       TimeSpan period)
                                   {
                                       _log = log;
                                       _subject = subject;
                                       _timer = new Timer(Tick, null, TimeSpan.Zero, period);
                                       _log.Info("Created");
                                   }
                               
                                   public IDisposable Subscribe(IObserver<Tick> observer)
                                   {
                                       var subscription = _subject.Subscribe(observer);
                                       _log.Info("Subscribed");
                                       return subscription;
                                   }
                               
                                   public void Dispose()
                                   {
                                       _timer.Dispose();
                                       _log.Info("Disposed");
                                   }
                               
                                   private void Tick(object? state) => _subject.OnNext(default);
                               }
                               
                               public abstract class ViewModel : INotifyPropertyChanged
                               {
                                   public required IDispatcher Dispatcher { private get; init; }
                               
                                   public required ILog<ViewModel> Log { private get; init; }
                               
                                   public event PropertyChangedEventHandler? PropertyChanged;
                               
                                   protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
                                   {
                                       Log.Info($"Property \"{propertyName}\" changed");
                                       if (PropertyChanged is {} propertyChanged)
                                       {
                                           Dispatcher.Dispatch(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName)));
                                       }
                                   }
                               }
                               
                               public interface IClockViewModel: INotifyPropertyChanged
                               {
                                   string Time { get; }
                               
                                   string Date { get; }
                               }
                               
                               public class ClockViewModel : ViewModel, IClockViewModel, IDisposable, IObserver<Tick>
                               {
                                   private readonly IClock _clock;
                                   private readonly ILog<ClockViewModel> _log;
                                   private readonly IDisposable _timerToken;
                                   private DateTimeOffset _now;
                               
                                   public ClockViewModel(
                                       ILog<ClockViewModel> log,
                                       IClock clock,
                                       Func<TimeSpan, IObservable<Tick>> timerFactory)
                                   {
                                       _log = log;
                                       _clock = clock;
                                       _now = _clock.Now;
                                       _timerToken = timerFactory(TimeSpan.FromSeconds(1)).Subscribe(this);
                                       log.Info("Created");
                                   }
                               
                                   public string Time => _now.ToString("T");
                               
                                   public string Date => _now.ToString("d");
                               
                                   void IDisposable.Dispose()
                                   {
                                       _timerToken.Dispose();
                                       _log.Info("Disposed");
                                   }
                               
                                   void IObserver<Tick>.OnNext(Tick value)
                                   {
                                       _now = _clock.Now;
                                       _log.Info("Tick");
                                       OnPropertyChanged(nameof(Date));
                                       OnPropertyChanged(nameof(Time));
                                   }
                               
                                   void IObserver<Tick>.OnError(Exception error) {}
                               
                                   void IObserver<Tick>.OnCompleted() {}
                               }
                               
                               internal partial class Service
                               {
                                    public Service(IClockViewModel clock)
                                    {
                                    }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<Service>("Root")
                                           .Bind().To<Subject<TT>>()
                                           .Bind().As(Lifetime.Singleton).To<ClockViewModel>()
                                           .Bind().As(Lifetime.Singleton).To<Metronome>()
                                           .Bind().As(Lifetime.PerBlock).To<Clock>()
                                           .Bind().To<Log<TT>>()
                                           .Bind().To<Dispatcher>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using var composition = new Composition();
                                       var service = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Latest));
        // Then
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningMetadataDefect && i.Locations.FirstOrDefault().GetSource() == "To<Metronome>()").ShouldBe(1, result);
    }
#endif
}