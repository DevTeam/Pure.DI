namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
/// Tests related to the overriding of bindings.
/// </summary>
public class OverrideTests
{
    [Theory]
    [InlineData("")]
    [InlineData(".Hint(Hint.ThreadSafe, \"On\")")]
    [InlineData(".Hint(Hint.ThreadSafe, \"Off\")")]
    public async Task ShouldSupportSimpleOverride(string hints)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           #hints#
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceA service);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.Replace("#hints#", hints).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA"], result);
    }

    [Fact]
    public async Task ShouldSupportSimpleOverrideWhenCyclicDependency()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                                    public LoggerA(IService service)
                                    {
                                    }
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceA service);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorCyclicDependency && i.Locations.FirstOrDefault().GetSource() == "LoggerA logger").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldSupportSeveralOverrides()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum Color { Red };
                               
                               interface IClock
                               {
                                   DateTimeOffset Now { get; }
                               }
                               
                               class Clock : IClock
                               {
                                   public DateTimeOffset Now => DateTimeOffset.Now;
                               }
                               
                               interface IDependency
                               {
                                   string Name { get; }
                               
                                   int Id { get; }
                               
                                   int SubId { get; }
                               }
                               
                               class Dependency : IDependency
                               {
                                   private readonly string _name;
                                   private readonly int _id;
                                   private readonly int _subId;
                                   
                                   public Dependency(
                                       string name,
                                       IClock clock,
                                       int id,
                                       [Tag("sub")] int subId,
                                       Color red)
                                   {
                                       _name = name;
                                       _id = id;
                                       _subId = subId;
                                   }
                                   public string Name => _name;
                               
                                   public int Id => _id;
                               
                                   public int SubId => _subId;
                               }
                               
                               interface IService
                               {
                                   IList<IDependency> Dependencies { get; }
                               }
                               
                               class Service : IService
                               {
                                   public Service(Func<int, int, IDependency> dependencyFactory)
                                   {
                                       Dependencies = new[]
                                       {
                                           dependencyFactory(0, 99),
                                           dependencyFactory(1, 99),
                                           dependencyFactory(2, 99)
                                       };
                                   }
                               
                                   public IList<IDependency> Dependencies { get; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                       .Bind(Tag.Red).To(_ => Color.Red)
                                       .Bind().As(Lifetime.Singleton).To<Clock>()
                                       .Bind().To<Func<int, int, IDependency>>(ctx =>
                                           (dependencyId, subId) =>
                                           {
                                               // Overrides with a lambda argument
                                               ctx.Override(dependencyId);
                                       
                                               // Overrides with tag using lambda argument
                                               ctx.Override(subId, "sub");
                                       
                                               // Overrides with some value
                                               ctx.Override($"Dep {dependencyId} {subId}");
                                       
                                               // Overrides with injected value
                                               ctx.Inject(Tag.Red, out Color red);
                                               ctx.Override(red);
                                       
                                               ctx.Inject<Dependency>(out var dependency);
                                               return dependency;
                                           })
                                       .Bind().To<Service>()
                                       .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependencies.Count);
                                       
                                       Console.WriteLine(service.Dependencies[0].Id);
                                       Console.WriteLine(service.Dependencies[0].SubId);
                                       Console.WriteLine(service.Dependencies[0].Name);
                                       
                                       Console.WriteLine(service.Dependencies[1].Id);
                                       Console.WriteLine(service.Dependencies[1].Name);
                                       
                                       Console.WriteLine(service.Dependencies[2].Id);
                                       Console.WriteLine(service.Dependencies[2].Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["3", "0", "99", "Dep 0 99", "1", "Dep 1 99", "2", "Dep 2 99"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideAfterCannotResolve()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IClock
                               {
                                   DateTimeOffset Now { get; }
                               }
                               
                               class Clock : IClock
                               {
                                   public DateTimeOffset Now => DateTimeOffset.Now;
                               }
                               
                               interface IDependency
                               {
                                   string Name { get; }
                                   IClock Clock { get; }
                                   IRepository Repository { get; }
                               }
                               
                               interface IRepository
                               {
                                   IClock Clock { get; }
                               }
                               
                               class Repository : IRepository
                               {
                                   public Repository(IClock clock)
                                   {
                                       Clock = clock;
                                   }
                               
                                   public IClock Clock { get; }
                               }
                               
                               class Dependency : IDependency
                               {
                                   private readonly string _name;
                               
                                   public Dependency(string name,
                                       IClock clock,
                                       IRepository repository)
                                   {
                                       _name = name;
                                       Clock = clock;
                                       Repository = repository;
                                   }
                               
                                   public string Name => _name;
                               
                                   public IClock Clock { get; }
                               
                                   public IRepository Repository { get; }
                               }
                               
                               interface IService
                               {
                                   IList<IDependency> Dependencies { get; }
                               }
                               
                               class Service : IService
                               {
                                   public Service(Func<int, int, IDependency> dependencyFactory)
                                   {
                                       Dependencies = new[]
                                       {
                                           dependencyFactory(0, 99),
                                           dependencyFactory(1, 99),
                                           dependencyFactory(2, 99)
                                       };
                                   }
                               
                                   public IList<IDependency> Dependencies { get; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Repository>()
                                           .Bind().To<Func<int, int, IDependency>>(ctx => (dependencyId, subId) =>
                                           {
                                               // this also should work ctx.Inject<Clock>(new Clock());
                                               ctx.Inject<Clock>(out var clock);
                                               ctx.Override<IClock>(clock);
                                               ctx.Override("Abc");
                                               ctx.Inject<Dependency>(out var dependency);
                                               return dependency;
                                           })
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportOverrideAfterCannotResolveAbstractDependency()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IClockDep
                               {
                               }
                               
                               class ClockDep: IClockDep
                               {
                               }
                           
                               interface IClock
                               {
                                   DateTimeOffset Now { get; }
                               }
                               
                               class Clock : IClock
                               {
                                   public Clock(IClockDep unresolvedDep)
                                   {
                                   }
                                   
                                   public DateTimeOffset Now => DateTimeOffset.Now;
                               }
                               
                               interface IDependency
                               {
                                   string Name { get; }
                                   IClock Clock { get; }
                                   IRepository Repository { get; }
                               }
                               
                               interface IRepository
                               {
                                   IClock Clock { get; }
                               }
                               
                               class Repository : IRepository
                               {
                                   public Repository(IClock clock)
                                   {
                                       Clock = clock;
                                   }
                               
                                   public IClock Clock { get; }
                               }
                               
                               class Dependency : IDependency
                               {
                                   private readonly string _name;
                               
                                   public Dependency(string name,
                                       IClock clock,
                                       IRepository repository)
                                   {
                                       _name = name;
                                       Clock = clock;
                                       Repository = repository;
                                   }
                               
                                   public string Name => _name;
                               
                                   public IClock Clock { get; }
                               
                                   public IRepository Repository { get; }
                               }
                               
                               interface IService
                               {
                                   IList<IDependency> Dependencies { get; }
                               }
                               
                               class Service : IService
                               {
                                   public Service(Func<int, int, IDependency> dependencyFactory)
                                   {
                                       Dependencies = new[]
                                       {
                                           dependencyFactory(0, 99),
                                           dependencyFactory(1, 99),
                                           dependencyFactory(2, 99)
                                       };
                                   }
                               
                                   public IList<IDependency> Dependencies { get; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Clock>()
                                           .Bind().To<Repository>()
                                           .Bind().To<Func<int, int, IDependency>>(ctx => (dependencyId, subId) =>
                                           {
                                               ctx.Override<IClock>(new Clock(new ClockDep()));
                                               ctx.Override("Abc");
                                               ctx.Inject<Dependency>(out var dependency);
                                               return dependency;
                                           })
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningMetadataDefect && i.Locations.FirstOrDefault().GetSource() == "To<Clock>()").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldSupportSimpleOverrideWhenHasNotTypeParam()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Logger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    Logger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(Logger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public Logger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   Logger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, Logger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public Logger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Override(new Logger());
                                                ctx.Inject(out ServiceA service);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Logger", "Sample.Logger"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenGeneric()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger<T> {}
                               
                               class LoggerA<T>: ILogger<T>
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger<int> Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger<int> logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger<int> Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger<string> Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger<string> logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger<string> Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA<int> logger1);
                                                ctx.Override<ILogger<int>>(logger1);
                                                
                                                ctx.Inject(out LoggerA<string> logger2);
                                                ctx.Override<ILogger<string>>(logger2);
                                                
                                                ctx.Inject(out ServiceA service);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA`1[System.String]", "Sample.LoggerA`1[System.Int32]"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenOpenGeneric()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger<T> {}
                               
                               class LoggerA<T>: ILogger<T>
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger<int> Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger<int> logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger<int> Logger { get; set; }
                               }
                           
                               interface IService<T>
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger<T> Logger { get; }
                               }
                           
                               class ServiceA<T>: IService<T>
                               {
                                   public ServiceA(IDependency dep, ILogger<T> logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger<T> Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService<TT>>().To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA<int> logger1);
                                                ctx.Override<ILogger<int>>(logger1);
                                                
                                                ctx.Inject(out LoggerA<TT> logger2);
                                                ctx.Override<ILogger<TT>>(logger2);
                                                
                                                ctx.Inject(out ServiceA<TT> service);
                                                return service;
                                           })
                                           .Root<IService<TT>>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root<string>();
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA`1[System.String]", "Sample.LoggerA`1[System.Int32]"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildUpWhenOverride()
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
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
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
                                   public IDependency? Dep { get; set; }
                                   
                                   [Dependency]
                                   public ILogger? Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenTag()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               class LoggerB: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency([Tag(99)] ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA loggerA);
                                                ctx.Override<ILogger>(loggerA);
                                                ctx.Inject(out LoggerB loggerB);
                                                ctx.Override<ILogger>(loggerB, 99);
                                                ctx.Inject(out ServiceA service);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerB"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenSeveralTags()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               class LoggerB: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency([Tag(99)] ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger, [Tag(33)] ILogger logger33)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA loggerA);
                                                ctx.Override<ILogger>(loggerA);
                                                ctx.Inject(out LoggerB loggerB);
                                                ctx.Override<ILogger>(loggerB, 33, 99);
                                                ctx.Inject(out ServiceA service);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerB"], result);
    }

    [Theory]
    [InlineData(Lifetime.Transient, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerB;Sample.LoggerB;Sample.LoggerC;Sample.LoggerC")]
    [InlineData(Lifetime.Scoped, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerB;Sample.LoggerA;Sample.LoggerC;Sample.LoggerA")]
    [InlineData(Lifetime.Singleton, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerB;Sample.LoggerA;Sample.LoggerC;Sample.LoggerA")]
    [InlineData(Lifetime.PerBlock, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerB;Sample.LoggerB;Sample.LoggerC;Sample.LoggerC")]
    [InlineData(Lifetime.PerResolve, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerB;Sample.LoggerB;Sample.LoggerC;Sample.LoggerC")]
    internal async Task ShouldSupportOverride(Lifetime lifetime, string output)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               class LoggerB: ILogger
                               {
                               }
                               
                               class LoggerC: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               class ServiceB: IService 
                               {
                                   public ServiceB(ILogger logger, IDependency dep)
                                   {
                                        Logger = logger;
                                        Dep = dep;
                                   }
                               
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               class ServiceC: IService 
                               {
                                   public ServiceC(IDependency dep)
                                   {
                                        Logger = new LoggerC();
                                        Dep = dep;
                                   }
                               
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind().As(Lifetime.#lifetime#).To<Dependency>()
                                           .Bind<IService>("A").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceA service);
                                                return service;
                                           })
                                           .Bind<IService>("B").As(Lifetime.#lifetime#).To(ctx => 
                                           {
                                                ctx.Inject(out LoggerB logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceB service);
                                                return service;
                                           })
                                           .Bind<IService>("C").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerC logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceC service);
                                                return service;
                                           })
                                           .Root<IService>("RootA", "A")
                                           .Root<IService>("RootB", "B")
                                           .Root<IService>("RootC", "C");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var rootA = composition.RootA;
                                       Console.WriteLine(rootA.Logger);
                                       Console.WriteLine(rootA.Dep.Logger);

                                       var rootB = composition.RootB;
                                       Console.WriteLine(rootB.Logger);
                                       Console.WriteLine(rootB.Dep.Logger);
                                       
                                       var rootC = composition.RootC;
                                       Console.WriteLine(rootC.Logger);
                                       Console.WriteLine(rootC.Dep.Logger);
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([..output.Split(";")], result);
    }

    [Theory]
    [InlineData(Lifetime.Transient, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerB;Sample.LoggerB;Sample.LoggerC;Sample.LoggerC")]
    [InlineData(Lifetime.PerBlock, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerB;Sample.LoggerB;Sample.LoggerC;Sample.LoggerC")]
    [InlineData(Lifetime.PerResolve, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerB;Sample.LoggerB;Sample.LoggerC;Sample.LoggerC")]
    internal async Task ShouldSupportOverrideWhenStaticRoot(Lifetime lifetime, string output)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               class LoggerB: ILogger
                               {
                               }
                               
                               class LoggerC: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               class ServiceB: IService 
                               {
                                   public ServiceB(ILogger logger, IDependency dep)
                                   {
                                        Logger = logger;
                                        Dep = dep;
                                   }
                               
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               class ServiceC: IService 
                               {
                                   public ServiceC(IDependency dep)
                                   {
                                        Logger = new LoggerC();
                                        Dep = dep;
                                   }
                               
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind().As(Lifetime.#lifetime#).To<Dependency>()
                                           .Bind<IService>("A").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceA service);
                                                return service;
                                           })
                                           .Bind<IService>("B").As(Lifetime.#lifetime#).To(ctx => 
                                           {
                                                ctx.Inject(out LoggerB logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceB service);
                                                return service;
                                           })
                                           .Bind<IService>("C").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerC logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceC service);
                                                return service;
                                           })
                                           .Root<IService>("RootA", "A", kind: RootKinds.Static)
                                           .Root<IService>("RootB", "B", kind: RootKinds.Static)
                                           .Root<IService>("RootC", "C", kind: RootKinds.Static);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var rootA = Composition.RootA;
                                       Console.WriteLine(rootA.Logger);
                                       Console.WriteLine(rootA.Dep.Logger);

                                       var rootB = Composition.RootB;
                                       Console.WriteLine(rootB.Logger);
                                       Console.WriteLine(rootB.Dep.Logger);
                                       
                                       var rootC = Composition.RootC;
                                       Console.WriteLine(rootC.Logger);
                                       Console.WriteLine(rootC.Dep.Logger);
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([..output.Split(";")], result);
    }

    [Theory]
    [InlineData(Lifetime.Transient, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerA;Sample.LoggerA;Sample.LoggerC;Sample.LoggerA")]
    [InlineData(Lifetime.Scoped, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerB;Sample.LoggerA;Sample.LoggerC;Sample.LoggerA")]
    [InlineData(Lifetime.Singleton, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerB;Sample.LoggerA;Sample.LoggerC;Sample.LoggerA")]
    [InlineData(Lifetime.PerBlock, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerA;Sample.LoggerA;Sample.LoggerC;Sample.LoggerA")]
    [InlineData(Lifetime.PerResolve, "Sample.LoggerA;Sample.LoggerA;Sample.LoggerA;Sample.LoggerA;Sample.LoggerC;Sample.LoggerA")]
    internal async Task ShouldSupportCascadingOverride(Lifetime lifetime, string output)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               class LoggerB: ILogger
                               {
                               }
                               
                               class LoggerC: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               class ServiceB: IService 
                               {
                                   public ServiceB(ILogger logger, IDependency dep)
                                   {
                                        Logger = logger;
                                        Dep = dep;
                                   }
                               
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               class ServiceC: IService 
                               {
                                   public ServiceC(IDependency dep)
                                   {
                                        Logger = new LoggerC();
                                        Dep = dep;
                                   }
                               
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.#lifetime#).To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out Dependency dep);
                                                return dep;
                                           })
                                           .Bind<IService>("A").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceA service);
                                                return service;
                                           })
                                           .Bind<IService>("B").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerB logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceB service);
                                                return service;
                                           })
                                           .Bind<IService>("C").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerC logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceC service);
                                                return service;
                                           })
                                           .Root<IService>("RootA", "A")
                                           .Root<IService>("RootB", "B")
                                           .Root<IService>("RootC", "C");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var rootA = composition.RootA;
                                       Console.WriteLine(rootA.Logger);
                                       Console.WriteLine(rootA.Dep.Logger);

                                       var rootB = composition.RootB;
                                       Console.WriteLine(rootB.Logger);
                                       Console.WriteLine(rootB.Dep.Logger);
                                       
                                       var rootC = composition.RootC;
                                       Console.WriteLine(rootC.Logger);
                                       Console.WriteLine(rootC.Dep.Logger);
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([..output.Split(";")], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenBindingExists()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               class LoggerB: ILogger
                               {
                               }
                               
                               class LoggerC: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               class ServiceB: IService 
                               {
                                   public ServiceB(ILogger logger, IDependency dep)
                                   {
                                        Logger = logger;
                                        Dep = dep;
                                   }
                               
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               class ServiceC: IService 
                               {
                                   public ServiceC(IDependency dep)
                                   {
                                        Logger = new LoggerC();
                                        Dep = dep;
                                   }
                               
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<LoggerC>()
                                           .Bind<IService>("A").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceA service);
                                                return service;
                                           })
                                           .Bind<IService>("B").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerB logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceB service);
                                                return service;
                                           })
                                           .Bind<IService>("C").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerC logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceC service);
                                                return service;
                                           })
                                           .Root<IService>("RootA", "A")
                                           .Root<IService>("RootB", "B")
                                           .Root<IService>("RootC", "C");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var rootA = composition.RootA;
                                       Console.WriteLine(rootA.Logger);
                                       Console.WriteLine(rootA.Dep.Logger);

                                       var rootB = composition.RootB;
                                       Console.WriteLine(rootB.Logger);
                                       Console.WriteLine(rootB.Dep.Logger);
                                       
                                       var rootC = composition.RootC;
                                       Console.WriteLine(rootC.Logger);
                                       Console.WriteLine(rootC.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA", "Sample.LoggerB", "Sample.LoggerB", "Sample.LoggerC", "Sample.LoggerC"], result);
    }

    [Theory]
    [InlineData(Lifetime.Transient)]
    [InlineData(Lifetime.Scoped)]
    [InlineData(Lifetime.Singleton)]
    [InlineData(Lifetime.PerBlock)]
    [InlineData(Lifetime.PerResolve)]
    internal async Task ShouldSupportOverrideWhenFunc(Lifetime lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               class LoggerB: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger, int num)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(int num, Func<IDependency> dep, ILogger logger)
                                   {
                                        Dep = dep();
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               class ServiceB: IService 
                               {
                                   public ServiceB(ILogger logger, IDependency dep)
                                   {
                                        Logger = logger;
                                        Dep = dep;
                                   }
                               
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().As(Lifetime.#lifetime#).To(ctx => 99)
                                           .Bind().To<Dependency>()
                                           .Bind<IService>("A").To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceA service);
                                                return service;
                                           })
                                           .Bind<IService>("B").As(Lifetime.#lifetime#).To(ctx => 
                                           {
                                                ctx.Inject(out LoggerB logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceB service);
                                                return service;
                                           })
                                           .Root<IService>("RootA", "A")
                                           .Root<IService>("RootB", "B");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var rootA = composition.RootA;
                                       Console.WriteLine(rootA.Logger);
                                       Console.WriteLine(rootA.Dep.Logger);

                                       var rootB = composition.RootB;
                                       Console.WriteLine(rootB.Logger);
                                       Console.WriteLine(rootB.Dep.Logger);
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA", "Sample.LoggerB", "Sample.LoggerB"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenFuncWithArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class Logger: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger, string name)
                                   {
                                        Logger = logger;
                                        Console.WriteLine(name);
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(Func<string, IDependency> dep, ILogger logger)
                                   {
                                        Dep = dep("Abc");
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Logger>()
                                           .Bind().To<Func<string, IDependency>>(ctx =>
                                           { 
                                                return new Func<string, IDependency>(name => 
                                                {
                                                    ctx.Override(name);
                                                    ctx.Inject(out Dependency dep);
                                                    return dep;
                                                });
                                           })
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenFuncWithArgAndDependency()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IClock
                               {
                                   DateTimeOffset Now { get; }
                               }
                               
                               class Clock : IClock
                               {
                                   public DateTimeOffset Now => DateTimeOffset.Now;
                               }
                               
                               interface IDependency
                               {
                                   int Id { get; }
                               
                                   int SubId { get; }
                               }
                               
                               class Dependency : IDependency
                               {
                                   private readonly int _id;
                                   private readonly int _subId;
                               
                                   public Dependency(
                                       IClock clock,
                                       int id,
                                       [Tag("sub")] int subId)
                                   {
                                       _id = id;
                                       _subId = subId;
                                   }
                               
                                   public int Id => _id;
                               
                                   public int SubId => _subId;
                               }
                               
                               interface IService
                               {
                                   List<IDependency> Dependencies { get; }
                               }
                               
                               class Service : IService
                               {
                                   public Service(Func<int, int, IDependency> dependencyFactory)
                                   {
                                       Dependencies = new List<IDependency>
                                       {
                                           dependencyFactory(0, 99),
                                           dependencyFactory(1, 99),
                                           dependencyFactory(2, 99)
                                       };
                                   }
                               
                                   public List<IDependency> Dependencies { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Clock>()
                                           .Bind().To<Func<int, int, IDependency>>(ctx =>
                                               (dependencyId, subId) =>
                                               {
                                                   ctx.Override(dependencyId);
                                                   ctx.Override(subId, "sub");
                                                   ctx.Inject<Dependency>(out var dependency);
                                                   return dependency;
                                               })
                                           .Bind().To<Service>()
                                           
                                           // Composition root
                                           .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependencies.Count);
                                       Console.WriteLine(service.Dependencies[0].Id);
                                       Console.WriteLine(service.Dependencies[1].Id);
                                       Console.WriteLine(service.Dependencies[2].Id);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["3", "0", "1", "2"], result);
    }

    [Fact]
    public async Task ShouldSupportStdFuncWithArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IClock
                               {
                                   DateTimeOffset Now { get; }
                               }
                               
                               class Clock : IClock
                               {
                                   public DateTimeOffset Now => DateTimeOffset.Now;
                               }
                               
                               interface IDependency
                               {
                                   int Id { get; }
                               }
                               
                               class Dependency : IDependency
                               {
                                   private readonly int _id;
                               
                                   public Dependency(IClock clock, int id)
                                   {
                                       _id = id;
                                   }
                               
                                   public int Id => _id;
                               }
                               
                               interface IService
                               {
                                   List<IDependency> Dependencies { get; }
                               }
                               
                               class Service : IService
                               {
                                   public Service(Func<int, IDependency> dependencyFactory)
                                   {
                                       Dependencies = new List<IDependency>
                                       {
                                           dependencyFactory(0),
                                           dependencyFactory(1),
                                           dependencyFactory(2)
                                       };
                                   }
                               
                                   public List<IDependency> Dependencies { get; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                          .Bind().As(Lifetime.Singleton).To<Clock>()
                                          .Bind().To<Dependency>()
                                          .Bind().To<Service>()
                                          .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependencies.Count);
                                       Console.WriteLine(service.Dependencies[0].Id);
                                       Console.WriteLine(service.Dependencies[1].Id);
                                       Console.WriteLine(service.Dependencies[2].Id);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["3", "0", "1", "2"], result);
    }

    [Fact]
    public async Task ShouldSupportStdFuncWith2Args()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IClock
                               {
                                   DateTimeOffset Now { get; }
                               }
                               
                               class Clock : IClock
                               {
                                   public DateTimeOffset Now => DateTimeOffset.Now;
                               }
                               
                               interface IDependency
                               {
                                   int Id { get; }
                                   string Name { get; }
                               }
                               
                               class Dependency : IDependency
                               {
                                   private readonly int _id;
                                   private readonly string _name;
                               
                                   public Dependency(IClock clock, int id, string name)
                                   {
                                       _id = id;
                                       _name = name;
                                   }
                               
                                   public int Id => _id;
                                   public string Name => _name;
                               }
                               
                               interface IService
                               {
                                   List<IDependency> Dependencies { get; }
                               }
                               
                               class Service : IService
                               {
                                   public Service(Func<string, int, IDependency> dependencyFactory)
                                   {
                                       Dependencies = new List<IDependency>
                                       {
                                           dependencyFactory("a", 0),
                                           dependencyFactory("b", 1),
                                           dependencyFactory("c", 2)
                                       };
                                   }
                               
                                   public List<IDependency> Dependencies { get; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                          .Bind().As(Lifetime.Singleton).To<Clock>()
                                          .Bind().To<Dependency>()
                                          .Bind().To<Service>()
                                          .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Dependencies.Count);
                                       Console.WriteLine(service.Dependencies[0].Name);
                                       Console.WriteLine(service.Dependencies[0].Id);
                                       Console.WriteLine(service.Dependencies[1].Name);
                                       Console.WriteLine(service.Dependencies[1].Id);
                                       Console.WriteLine(service.Dependencies[2].Name);
                                       Console.WriteLine(service.Dependencies[2].Id);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["3", "a", "0", "b", "1", "c", "2"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenCtor()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Override<ILogger>(new Sample.LoggerA());
                                                ctx.Inject(out ServiceA service);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenInjectedOverridingAbstraction()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<LoggerA>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out ILogger logger);
                                                ctx.Override(logger);
                                                ctx.Inject(out ServiceA service);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenRootArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .RootArg<ILogger>("logger")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out ILogger logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceA service);
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
                                       var root = composition.Root(new LoggerA());
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenClassArg()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Arg<ILogger>("logger")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out ILogger logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceA service);
                                                return service;
                                           })
                                           .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(new LoggerA());
                                       var root = composition.Root;
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildUpOverrideWhenInjectAbstraction()
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
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
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
                                   public IDependency? Dep { get; set; }
                                   
                                   [Dependency]
                                   public ILogger? Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<LoggerA>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out ILogger logger);
                                                ctx.Override<ILogger>(logger);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildUpOverrideWhenCtor()
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
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger)
                                   {
                                        Logger = logger;
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
                                   public IDependency? Dep { get; set; }
                                   
                                   [Dependency]
                                   public ILogger? Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Override<ILogger>(new LoggerA());
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWhenInitialInjectionsHasNoAnyConstructors()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ILogger {}
                               
                               class LoggerBase: ILogger
                               {
                                    protected LoggerBase(int num)
                                    {
                                    }
                               }
                               
                               class LoggerA: LoggerBase
                               {
                                    public LoggerA(): base(33)
                                    {
                                    }
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(LoggerBase logger)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Override<LoggerBase>(logger);
                                                ctx.Inject(out ServiceA service);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA"], result);
    }

    [Theory]
    [InlineData("MyEnum", "MyEnum.A")]
    [InlineData("int", "33")]
    [InlineData("char", "'a'")]
    [InlineData("Type", "typeof(int)")]
    [InlineData("int", "int.Max(1, 2)")]
    public async Task ShouldSupportOverrideValue(string type, string value)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public enum MyEnum
                               {
                                   None,
                                   A,
                                   B
                               }
                           
                               interface ILogger {}
                               
                               class LoggerA: ILogger
                               {
                               }
                               
                               interface IDependency 
                               {
                                    ILogger Logger { get; }
                               }
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(ILogger logger, #type# val)
                                   {
                                        Logger = logger;
                                   }
                                   
                                   public ILogger Logger { get; set; }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                                   
                                   ILogger Logger { get; }
                               }
                           
                               class ServiceA: IService 
                               {
                                   public ServiceA(IDependency dep, #type# val, ILogger logger)
                                   {
                                        Dep = dep;
                                        Logger = logger;
                                   }
                           
                                   public IDependency Dep { get; set; }
                                   
                                   public ILogger Logger { get; set; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind<IService>().To(ctx => 
                                           {
                                                ctx.Override(#value#);
                                                ctx.Inject(out LoggerA logger);
                                                ctx.Override<ILogger>(logger);
                                                ctx.Inject(out ServiceA service);
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
                                       Console.WriteLine(root.Logger);
                                       Console.WriteLine(root.Dep.Logger);
                                   }
                               }
                           }
                           """.Replace("#type#", type).Replace("#value#", value).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.LoggerA", "Sample.LoggerA"], result);
    }

    [Theory]
    [InlineData(Lifetime.Singleton, "33")]
    [InlineData(Lifetime.Scoped, "33")]
    [InlineData(Lifetime.PerResolve, "33")]
    [InlineData(Lifetime.PerBlock, "99")]
    [InlineData(Lifetime.Transient, "99")]
    internal async Task ShouldNotOverrideTopLevelDependencies(Lifetime lifetime, string secondId)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency
                               {
                                   int Id { get; }
                               }
                               
                               class Dependency : IDependency
                               {
                                   public Dependency(int id)
                                   {
                                       Id = id;
                                   }
                                   public int Id { get; }
                               }
                               
                               interface IService
                               {
                                   IReadOnlyList<IDependency> Dependencies { get; }
                               }
                               
                               class Service : IService
                               {
                                   public Service(Func<int, IDependency> dependencyFactoryWithArgs)
                                   {
                                       Dependencies = new List<IDependency>
                                       {
                                           dependencyFactoryWithArgs(33),
                                           dependencyFactoryWithArgs(99)
                                       }.AsReadOnly();
                                   }
                               
                                   public IReadOnlyList<IDependency> Dependencies { get; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                          .Bind().As(Lifetime.#lifetime#).To<int>(_ => 77)
                                          .Bind().As(Lifetime.#lifetime#).To<Dependency>()
                                          .Bind().To<Service>()
                               
                                          // Composition root
                                          .Root<(IService Service, int Id)>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       var service = root.Service;
                                       var dependencies = service.Dependencies;
                                       Console.WriteLine(dependencies.Count);
                                       Console.WriteLine(dependencies[0].Id);
                                       Console.WriteLine(dependencies[1].Id);
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2", "33", secondId], result);
    }

    [Theory]
    [InlineData(Lifetime.Singleton)]
    [InlineData(Lifetime.Scoped)]
    [InlineData(Lifetime.PerResolve)]
    [InlineData(Lifetime.PerBlock)]
    [InlineData(Lifetime.Transient)]
    internal async Task ShouldNotOverrideUnresolvedTopLevelDependencies(Lifetime lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency
                               {
                                   int Id { get; }
                               }
                               
                               class Dependency : IDependency
                               {
                                   public Dependency(int id)
                                   {
                                       Id = id;
                                   }
                                   public int Id { get; }
                               }
                               
                               interface IService
                               {
                                   IReadOnlyList<IDependency> Dependencies { get; }
                               }
                               
                               class Service : IService
                               {
                                   public Service(Func<int, IDependency> dependencyFactoryWithArgs)
                                   {
                                       Dependencies = new List<IDependency>
                                       {
                                           dependencyFactoryWithArgs(33),
                                           dependencyFactoryWithArgs(99)
                                       }.AsReadOnly();
                                   }
                               
                                   public IReadOnlyList<IDependency> Dependencies { get; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                          .Bind().As(Lifetime.#lifetime#).To<Dependency>()
                                          .Bind().To<Service>()
                               
                                          // Composition root
                                          .Root<(IService Service, int Id)>("Root");
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
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(1, result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldOverrideByMember()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Linq;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               public interface IRepository
                               {
                                    int Id { get; }
                               }
                               
                               internal class ServiceRepository : IRepository
                               {
                                    public int Id { get; } = 10;
                               }
                               
                               internal class GlobalRepository : IRepository
                               {
                                    public int Id { get; } = 20;
                               }
                               
                               public interface IService
                               {
                                   IRepository Repository { get; }
                               }
                               
                               internal class MyService : IService
                               {
                                   public MyService(int id, IRepository repository)
                                   {
                                       Repository = repository;
                                       Console.WriteLine(id);
                                   }
                               
                                   public IRepository Repository { get; }
                               }
                               
                               public partial class HostComposition
                               {
                                   // public partial IRepository Repository { get; }
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(HostComposition))
                                           .Bind<IRepository>().As(Lifetime.Singleton).To<GlobalRepository>()
                                           .Root<IRepository>("Repository")
                                           .Bind(1).To<IService>(ctx =>
                                           {
                                               ctx.Inject(out HostComposition hostComposition);
                               
                                               var serviceComposition = new ServiceComposition(hostComposition);
                                               return serviceComposition.CreateService();
                                           })
                                           .Bind<IEnumerable<IService>>().To(ctx =>
                                           {
                                               ctx.Inject<IService>(1, out var s1);
                                               return new[] { s1 };
                                           })
                                           .Root<IEnumerable<IService>>("Services");
                                   }
                               }
                               
                               public partial class ServiceComposition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(ServiceComposition))
                                           .Arg<HostComposition>("hostComposition")
                                           .Bind<IService>().As(Lifetime.Scoped).To<MyService>(ctx =>
                                           {
                                               ctx.Inject(out HostComposition hostComposition);
                                               ctx.Override<IRepository>(hostComposition.Repository);
                                               ctx.Override<int>(hostComposition.Repository.Id);
                                               ctx.Inject(out MyService myService);
                                               return myService;
                                           })
                                           .Root<Func<IService>>("CreateService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var hostComposition = new HostComposition();
                                       var service1 = hostComposition.Services.First();
                                       Console.WriteLine(service1.Repository);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["20", "Sample.GlobalRepository"], result);
    }

    [Fact]
    public async Task ShouldSupportOverrideWithMemberAccessor()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               partial class Composition
                               {
                                   public void Setup()
                                   {
                                       DI.Setup()
                                           .Arg<string>("config")
                                           .DefaultLifetime(Lifetime.Singleton)
                                           .Bind().To((string configJson) => new Config(new ServiceConfig("a")))
                                           .Bind(123).To<Service>()
                                           .Bind().To(cxt =>
                                           {
                                               cxt.Inject(out Config config);
                                               cxt.Override<ServiceConfig>(config.ServiceConfig1);
                                               cxt.Inject(123, out Service service);
                                               return service;
                                           })
                               
                                           .Bind().To<ServiceWrapper>()
                                           .Root<ServiceWrapper>(nameof(ServiceWrapper));
                                   }
                               }
                               
                               public sealed class ServiceWrapper
                               {
                                   public Service Service1 { get; }
                               
                                   public ServiceWrapper(Service service1)
                                   {
                                       Service1 = service1;
                                   }
                               }
                               
                               public sealed class Service
                               {
                                   public ServiceConfig Config { get; }
                               
                                   public Service(ServiceConfig config) => Config = config;
                               }
                               
                               public sealed class ServiceConfig
                               {
                                   public string Config { get; }
                                   
                                   public ServiceConfig(string config)
                                   {
                                       Config = config;
                                   }
                               }
                               
                               public sealed class Config
                               {
                                   public ServiceConfig ServiceConfig1 { get; }
                                   
                                   public Config(ServiceConfig serviceConfig1)
                                   {
                                       ServiceConfig1 = serviceConfig1;
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("JSON");
                                       var wrapper = composition.ServiceWrapper;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
}