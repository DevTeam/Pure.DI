namespace Pure.DI.IntegrationTests;

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
}