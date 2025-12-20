namespace Pure.DI.IntegrationTests;

using Core;

public class ErrorsAndWarningsTests
{
    [Fact]
    public async Task ShouldShowWarningsForGenericRootWhenResolveMethods()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               class Dep<T> { }
                           
                               interface IBox<T> { T? Content { get; set; } }
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(Dep<T> dep)
                                   {
                                   }
                                   
                                   public T? Content { get; set; }
                               }
                           
                               internal partial class Composition
                               {
                                   void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           // Composition Root
                                           .Root<IBox<TT>>("GetRoot");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.GetRoot<int>();
                                       Console.WriteLine(root);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0);
        result.Warnings.Count.ShouldBe(1);
        result.Warnings.Count(i => i.Id == LogId.WarningTypeArgInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IBox<TT>>(\"GetRoot\")").ShouldBe(1);
        result.StdOut.ShouldBe(["Sample.CardboardBox`1[System.Int32]"], result);
    }

    [Fact]
    public async Task ShouldShowCompilationErrorWhenArgIsBasedOnGenericMarker()
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
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep, string name)
                                   { 
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<IList<TT>>("serviceName")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(2, result);
        result.Errors.Count(i => i is { Id: LogId.ErrorInvalidMetadata, Message: "The composition argument type cannot be based on a generic type marker." } && i.Locations.FirstOrDefault().GetSource() == "Arg<IList<TT>>(\"serviceName\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowCompilationErrorWhenArgIsGenericMarker()
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
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep, string name)
                                   { 
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<TT>("serviceName")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(2, result);
        result.Errors.Count(i => i is { Id: LogId.ErrorInvalidMetadata, Message: "The composition argument type cannot be based on a generic type marker." } && i.Locations.FirstOrDefault().GetSource() == "Arg<TT>(\"serviceName\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowErrorWhenInheritedTypeNotFound()
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
                               }
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Builders<IService>();
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorInvalidMetadata && i.Locations.FirstOrDefault().GetSource() == "Builders<IService>()").ShouldBe(1, result);
    }

    [Theory]
    [InlineData("ctx2")]
    [InlineData("Console")]
    public async Task ShouldRaiseCompilationErrorWhenContextIsUsingDirectly(string contextArgName)
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
                                   public Service(IDependency dep)
                                   { 
                                       Dep = dep;
                                       Console.WriteLine("Created");
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               internal partial class Composition
                               {
                                   private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime) 
                                   {
                                       return value;      
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // OnDependencyInjection = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To(#ctx# => {
                                               System.Console.WriteLine(#ctx#);
                                               ctx.Inject<IDependency>(out var dependency);
                                               return new Service(dependency);
                                           })
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();      
                                       var service = composition.Service;     
                                   }
                               }
                           }
                           """.Replace("#ctx#", contextArgName).RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors
            .Count(i => i is { Id: LogId.ErrorInvalidMetadata } && i.Message == $"It is not possible to use \"{contextArgName}\" directly. Only its methods or properties can be used." && i.Locations.FirstOrDefault().GetSource() == contextArgName)
            .ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldRaiseCompilationErrorWhenContextIsUsingDirectlyNegative()
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
                                   public Service(IDependency dep)
                                   { 
                                       Dep = dep;
                                       Console.WriteLine("Created");
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               internal partial class Composition
                               {
                                   private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime) 
                                   {
                                       return value;      
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // OnDependencyInjection = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To(WriteLine => {
                                               System.Console.WriteLine(ctx2);
                                               ctx.Inject<IDependency>(out var dependency);
                                               return new Service(dependency);
                                           })
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();      
                                       var service = composition.Service;     
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9, CheckCompilationErrors: false));

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors
            .Count(i => i is { Id: LogId.ErrorInvalidMetadata, Message: "It is not possible to use \"ctx2\" directly. Only its methods or properties can be used." } && i.Locations.FirstOrDefault().GetSource() == "ctx2")
            .ShouldBe(0, result);
    }

    [Fact]
    public async Task ShouldShowCompilationErrorWhenAsyncKeyword()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
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
                                   public Service(Task<IDependency> dep)
                                   { 
                                       Dep = dep.Result;
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Task<IDependency>>().To(async ctx => {
                                               await Task.Delay(0);
                                               return (IDependency)new Dependency();
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
                                       var service1 = composition.Service;
                                       var service2 = composition.Service;
                                       Console.WriteLine(service1.Dep != service2.Dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(1, result);
        result.Errors
            .Count(i => i is { Id: LogId.ErrorInvalidMetadata, Message: "Asynchronous factory with the async keyword is not supported." } && i.Locations.FirstOrDefault().GetSource() == "async")
            .ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldHandleCyclicDependencies()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                           
                               internal interface IService
                               {
                               }
                           
                               internal interface IDependency1
                               {
                               }
                           
                               internal interface IDependency2
                               {
                               }
                           
                               internal class Service : IService
                               {
                                   internal Service(IDependency1 dep) {}
                               }
                           
                               internal class Dependency1 : IDependency1
                               {
                                   public Dependency1(IDependency2 dep) {}
                               }
                           
                               internal class Dependency2 : IDependency2
                               {
                                   public Dependency2(IService service) {}
                               }
                           
                               internal partial class Composition
                               {
                                   void Setup() => Pure.DI.DI.Setup("Composition")
                                       .Bind<IDependency1>().To<Dependency1>()
                                       .Bind<IDependency2>().To<Dependency2>()
                                       .Bind<IService>().To<Service>()
                                       .Root<IService>("Service"); 
                               }
                           
                               public class Program { public static void Main() { } }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorCyclicDependency && i.Locations.FirstOrDefault().GetSource() == "dep").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldHandleCyclicDependenciesWhenFactory()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using System.Collections.Generic;
                               using Pure.DI;
                               using Sample;
                           
                               public interface IStartupStep
                               {
                                   void Start();
                               }
                               
                               public class StartupStep : IStartupStep
                               {
                                   public void Start()
                                   {
                                   }
                               }
                               
                               public class StepsConsumer : IStartupStep
                               {
                                   private readonly IEnumerable<Func<IStartupStep>> _stepsFactory;
                               
                                   public StepsConsumer(IEnumerable<Func<IStartupStep>> stepsFactory)
                                   {
                                       _stepsFactory = stepsFactory;
                                   }
                               
                                   public void Start()
                                   {
                                       foreach (var step in _stepsFactory)
                                       {
                                           step();
                                       }
                                   }
                               }
                           
                               internal partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup("Composition")
                                           .Bind().To<StartupStep>()
                                           .Bind().To(ctx => {
                                               ctx.Inject(out Func<IStartupStep> factory);
                                               return factory;
                                           })
                                           .Root<StepsConsumer>("Consumer");
                               }
                           
                               public class Program { public static void Main() { } }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorCyclicDependency && i.Locations.FirstOrDefault().GetSource() == "Func<IStartupStep> factory").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowErrorWhenUnresolvedDependency()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                           
                               internal interface IService
                               {
                               }
                           
                               internal interface IDependency
                               {
                               }
                           
                               internal class Service : IService
                               {
                                   internal Service(IDependency dependency)
                                   {
                                   }
                               }
                           
                               internal class Dependency : IDependency
                               {
                                   public Dependency(int id, string abc) {}
                               }
                           
                               internal partial class Composition
                               {                   
                                   void Setup() => Pure.DI.DI.Setup("Composition")
                                       .Bind<int>().To(_ => 99)
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<IService>().To<Service>().Root<IService>("Root"); 
                               }
                           
                               public class Program { public static void Main() { } }       
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Logs.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
        result.Logs.Count(i => i.Id == LogId.ErrorUnableToResolve && i.Locations.FirstOrDefault().GetSource() == "abc").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowMinimumNumbersOfErrorsWhenUnresolvedDependency()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                           
                               internal interface IService
                               {
                               }
                           
                               internal interface IDependency
                               {
                               }
                           
                               internal class Service : IService
                               {
                                   internal Service(IDependency dependency, int id, string abc)
                                   {
                                   }
                                   
                                   internal Service(int id, string abc)
                                   {
                                   }
                               }
                           
                               internal class Dependency : IDependency
                               {
                                   public Dependency(int id, string abc) {}
                               }
                           
                               internal partial class Composition
                               {                   
                                   void Setup() => Pure.DI.DI.Setup("Composition")
                                       .Bind<int>().To(_ => 99)
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<IService>().To<Service>().Root<IService>("Root"); 
                               }
                           
                               public class Program { public static void Main() { } }       
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Logs.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(1, result);
        result.Logs.Count(i => i.Id == LogId.ErrorUnableToResolve && i.Locations.FirstOrDefault().GetSource() == "abc").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowErrorWhenSingletonIndirectInjectsScoped()
    {
        // Given

        // When
        var result = await """
                               using System;
                               using Pure.DI;
                               using static Pure.DI.Lifetime; 
                           
                               namespace Sample
                               {
                               interface ISingletonDep
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               class SingletonDep
                                   : ISingletonDep, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IScopedDep
                               {
                                   ISingletonDep SingletonDep { get; }
                           
                                   bool IsDisposed { get; }
                               }
                           
                               class ScopedDep : IScopedDep, IDisposable
                               {
                                   private readonly ISingletonDep _singletonDep;
                           
                                   public ScopedDep(ISingletonDep singletonDep)
                                   {
                                       _singletonDep = singletonDep;
                                   }
                           
                                   public ISingletonDep SingletonDep => _singletonDep;
                                   
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IService
                               {
                                   IScopedDep ScopedDep { get; }
                               }
                           
                               class Service : IService
                               {
                                   private readonly IScopedDep _scopedDep;
                           
                                   public Service(Func<IScopedDep> scopedDep)
                                   {
                                       _scopedDep = scopedDep();
                                   }
                           
                                   public IScopedDep ScopedDep => _scopedDep;
                               }
                           
                               // Implements a session
                               class Session : Composition
                               {
                                   public Session(Composition composition) : base(composition)
                                   {
                                   }
                               }
                           
                               class ProgramRoot
                               {
                                   private readonly ISingletonDep _singletonDep;
                                   private readonly Func<Session> _sessionFactory;
                           
                                   public ProgramRoot(ISingletonDep singletonDep,
                                       Func<Session> sessionFactory)
                                   {
                                       _singletonDep = singletonDep;
                                       _sessionFactory = sessionFactory;
                                   }
                           
                                   public ISingletonDep SingletonDep => _singletonDep;
                                   
                                   public Session CreateSession() => _sessionFactory();
                               }
                           
                               partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<ISingletonDep>()
                                               .As(Singleton)
                                               .To<SingletonDep>()
                                           .Bind<IScopedDep>()
                                               .As(Scoped)
                                               .To<ScopedDep>()
                                           .Bind<IService>()
                                               .As(Singleton)
                                               .To<Service>()
                                           .Root<IService>("SessionRoot")
                                           .Root<ProgramRoot>("ProgramRoot");
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var program = composition.ProgramRoot;
                           
                                       // Creates session #1
                                       var session1 = program.CreateSession();
                                       var scopedDepInSession1 = session1.SessionRoot.ScopedDep;
                                       System.Console.WriteLine(scopedDepInSession1 == session1.SessionRoot.ScopedDep);
                           
                                       // Checks that the singleton instances are identical
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep == program.SingletonDep);
                           
                                       // Creates session #2
                                       var session2 = program.CreateSession();
                                       var scopedDepInSession2 = session2.SessionRoot.ScopedDep;
                                       System.Console.WriteLine(scopedDepInSession2 == session2.SessionRoot.ScopedDep);
                           
                                       // Checks that the scoped instances are not identical in different sessions
                                       System.Console.WriteLine(scopedDepInSession1 == scopedDepInSession2);
                           
                                       // Checks that the singleton instances are identical in different sessions
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep == scopedDepInSession2.SingletonDep);
                           
                                       // Disposes of session #1
                                       session1.Dispose();
                                       // Checks that the scoped instance is finalized
                                       System.Console.WriteLine(scopedDepInSession1.IsDisposed);
                           
                                       // Session #2 is still not finalized
                                       System.Console.WriteLine(session2.SessionRoot.ScopedDep.IsDisposed);
                           
                                       // Disposes of session #2
                                       session2.Dispose();
                                       // Checks that the scoped instance is finalized
                                       System.Console.WriteLine(scopedDepInSession2.IsDisposed);
                           
                                       // Disposes of composition
                                       composition.Dispose();
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep.IsDisposed);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(1, result);
        result.Errors.Count(i => i.Id == LogId.ErrorLifetimeDefect && i.Locations.FirstOrDefault().GetSource() == "To<ScopedDep>()").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowErrorWhenSingletonInjectsScoped()
    {
        // Given

        // When
        var result = await """
                               using System;
                               using Pure.DI;
                               using static Pure.DI.Lifetime; 
                           
                               namespace Sample
                               {
                               interface ISingletonDep
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               class SingletonDep
                                   : ISingletonDep, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IScopedDep
                               {
                                   ISingletonDep SingletonDep { get; }
                           
                                   bool IsDisposed { get; }
                               }
                           
                               class ScopedDep : IScopedDep, IDisposable
                               {
                                   private readonly ISingletonDep _singletonDep;
                           
                                   public ScopedDep(ISingletonDep singletonDep)
                                   {
                                       _singletonDep = singletonDep;
                                   }
                           
                                   public ISingletonDep SingletonDep => _singletonDep;
                                   
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IService
                               {
                                   IScopedDep ScopedDep { get; }
                               }
                           
                               class Service : IService
                               {
                                   private readonly IScopedDep _scopedDep;
                           
                                   public Service(IScopedDep scopedDep)
                                   {
                                       _scopedDep = scopedDep;
                                   }
                           
                                   public IScopedDep ScopedDep => _scopedDep;
                               }
                           
                               // Implements a session
                               class Session : Composition
                               {
                                   public Session(Composition composition) : base(composition)
                                   {
                                   }
                               }
                           
                               class ProgramRoot
                               {
                                   private readonly ISingletonDep _singletonDep;
                                   private readonly Func<Session> _sessionFactory;
                           
                                   public ProgramRoot(ISingletonDep singletonDep,
                                       Func<Session> sessionFactory)
                                   {
                                       _singletonDep = singletonDep;
                                       _sessionFactory = sessionFactory;
                                   }
                           
                                   public ISingletonDep SingletonDep => _singletonDep;
                                   
                                   public Session CreateSession() => _sessionFactory();
                               }
                           
                               partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<ISingletonDep>()
                                               .As(Singleton)
                                               .To<SingletonDep>()
                                           .Bind<IScopedDep>()
                                               .As(Scoped)
                                               .To<ScopedDep>()
                                           .Bind<IService>()
                                               .As(Singleton)
                                               .To<Service>()
                                           .Root<IService>("SessionRoot")
                                           .Root<ProgramRoot>("ProgramRoot");
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var program = composition.ProgramRoot;
                           
                                       // Creates session #1
                                       var session1 = program.CreateSession();
                                       var scopedDepInSession1 = session1.SessionRoot.ScopedDep;
                                       System.Console.WriteLine(scopedDepInSession1 == session1.SessionRoot.ScopedDep);
                           
                                       // Checks that the singleton instances are identical
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep == program.SingletonDep);
                           
                                       // Creates session #2
                                       var session2 = program.CreateSession();
                                       var scopedDepInSession2 = session2.SessionRoot.ScopedDep;
                                       System.Console.WriteLine(scopedDepInSession2 == session2.SessionRoot.ScopedDep);
                           
                                       // Checks that the scoped instances are not identical in different sessions
                                       System.Console.WriteLine(scopedDepInSession1 == scopedDepInSession2);
                           
                                       // Checks that the singleton instances are identical in different sessions
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep == scopedDepInSession2.SingletonDep);
                           
                                       // Disposes of session #1
                                       session1.Dispose();
                                       // Checks that the scoped instance is finalized
                                       System.Console.WriteLine(scopedDepInSession1.IsDisposed);
                           
                                       // Session #2 is still not finalized
                                       System.Console.WriteLine(session2.SessionRoot.ScopedDep.IsDisposed);
                           
                                       // Disposes of session #2
                                       session2.Dispose();
                                       // Checks that the scoped instance is finalized
                                       System.Console.WriteLine(scopedDepInSession2.IsDisposed);
                           
                                       // Disposes of composition
                                       composition.Dispose();
                                       System.Console.WriteLine(scopedDepInSession1.SingletonDep.IsDisposed);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(1, result);
        result.Errors.Count(i => i.Id == LogId.ErrorLifetimeDefect && i.Locations.FirstOrDefault().GetSource() == "To<ScopedDep>()").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowErrorWhenInheritedTypeNotFoundWhenRoots()
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
                               }
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Roots<IService>();
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorInvalidMetadata && i.Locations.FirstOrDefault().GetSource() == "Roots<IService>()").ShouldBe(1, result);
    }

    [Theory]
    [InlineData(DiagnosticSeverity.Error, LogId.ErrorInvalidMetadata)]
    [InlineData(DiagnosticSeverity.Warning, LogId.WarningMetadataDefect)]
    [InlineData(DiagnosticSeverity.Info, LogId.InfoMetadataDefect)]
    [InlineData(DiagnosticSeverity.Hidden, "")]
    internal async Task ShouldReportWhenContractWasNotImplemented(DiagnosticSeverity severity, string logId)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                                interface IService
                                {
                                }
                           
                                class Service 
                                {
                                }
                           
                                static class Setup
                                {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.SeverityOfNotImplementedContract, "#severity#")
                                           .Bind<IService>().To<Service>()
                                           .Root<Service>("MyRoot");
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
                           """.Replace("#severity#", severity.ToString()).RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(severity == DiagnosticSeverity.Error ? 1 : 0, result);
        if (severity != DiagnosticSeverity.Hidden)
        {
            result.Logs
                .Count(i => i.Severity == severity && i.Id == logId && i.Message.Contains("Sample.Service does not implement Sample.IService.") && i.Locations.FirstOrDefault().GetSource() == "To<Service>()")
                .ShouldBe(1, result);
        }
    }

    [Fact]
    public async Task ShouldShowErrorWhenCannotResolveRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                                interface IService
                                {
                                }
                           
                                class Service: IService 
                                {
                                }
                           
                                static class Setup
                                {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>(1).To<Service>()
                                           .Root<IService>("MyRoot1");
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
        result.Success.ShouldBeFalse(result);
        result.Errors
            .Count(i => i is { Id: LogId.ErrorUnableToResolve, Message: "Unable to resolve \"Sample.IService\" in Sample.IService() MyRoot1." })
            .ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowWarningWhenBindingWasNotUsed()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency { }
                           
                               internal class Dependency : IDependency { }
                           
                               internal interface IService { }
                           
                               internal class Service : IService { }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Root");
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningMetadataDefect && i.Locations.FirstOrDefault().GetSource() == "To<Dependency>()").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowWarningWhenBindingWasNotUsedForTheSameTag()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency { }

                               internal class Dependency : IDependency { }

                               internal interface IService { }

                               internal class Service : IService { }
                               
                               internal class Service2 : IService { }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Service>()
                                           .Bind().To<Service2>()
                                           .Root<IService>("Root");
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding && i.Locations.FirstOrDefault().GetSource() == "To<Service2>()").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningMetadataDefect && i.Locations.FirstOrDefault().GetSource() == "To<Service>()").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowWarningWhenBindingWasNotUsedForOtherTag()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency { }

                               internal class Dependency : IDependency { }

                               internal interface IService { }

                               internal class Service : IService { }
                               
                               internal class Service2 : IService { }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Service>()
                                           .Bind("Abc").To<Service2>()
                                           .Root<IService>("Root");
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningMetadataDefect && i.Locations.FirstOrDefault().GetSource() == "To<Service2>()").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowErrorWhenCannotResolveByTag()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency { }
                           
                               internal class Dependency : IDependency { }
                           
                               internal interface IService
                               {
                                   public IDependency Dependency1 { get; }
                                           
                                   public IDependency Dependency2 { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   public Service(Func<IDependency> dependency1, [Tag(123)] IDependency dependency2)
                                   {
                                       Dependency1 = dependency1();
                                       Dependency2 = dependency2;
                                   }
                           
                                   public IDependency Dependency1 { get; }
                                           
                                   public IDependency Dependency2 { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IDependency>("Dependency")
                                           .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Root;
                                       var service2 = composition.Root;
                                       Console.WriteLine(service1.Dependency1 == service1.Dependency2);        
                                       Console.WriteLine(service2.Dependency1 == service1.Dependency1);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve && i.Locations.FirstOrDefault().GetSource() == "dependency2").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowWarningWhenTagOnWasNotUsed()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                               
                               internal class Dep { }
                           
                               internal interface IService { }
                           
                               internal class Service: IService
                               {
                                   public Service(Dep dep) { }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind().To<Dep>()
                                           .Bind(Tag.On("Sample.Service.Service:abc")).To<Dep>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root"); 
                               }
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Root);
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningMetadataDefect && i.Locations.FirstOrDefault().GetSource() == "\"Sample.Service.Service:abc\"").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningMetadataDefect && i.Locations.FirstOrDefault().GetSource() == "To<Dep>()").ShouldBe(1, result);
        result.StdOut.ShouldBe(["Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportCannotResolveWhenTuple()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency { }
                           
                               internal class Dependency : IDependency { }
                           
                               internal readonly record struct Point(int X, int Y);
                           
                               internal interface IService
                               {
                                   IDependency Dependency { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   private readonly IDependency _dependency;
                           
                                   public Service((Point Point, IDependency Dependency) dep)
                                   {
                                       _dependency = dep.Dependency;
                                   }
                           
                                   public IDependency Dependency => _dependency;
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Point>().To(_ => new Point(7, 9))
                                           .Bind<IService>().To<Service>()
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
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeFalse(result);
        result.Logs.Count(i => i.Id == LogId.ErrorUnableToResolve && i.Locations.FirstOrDefault().GetSource() == "To<Service>()").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowTypeCannotBeInferredErrorWhenTypeCannotBeInferred()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               public interface IRepository
                               {
                               }
                               
                               internal class ServiceRepository : IRepository
                               {
                               }
                               
                               internal class GlobalRepository : IRepository
                               {
                               }
                               
                               public interface IService
                               {
                                   IRepository Repository { get; }
                               }
                               
                               internal class MyService : IService
                               {
                                   public MyService(IRepository repository)
                                   {
                                       Repository = repository;
                                   }
                               
                                   public IRepository Repository { get; }
                               }
                               
                               public partial class HostComposition
                               {
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
                                               var repo = hostComposition.Repository;
                                               ctx.Override(repo);
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
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeFalse(result);
        result.Logs.Count(i => i.Id == LogId.ErrorTypeCannotBeInferred && i.Locations.FirstOrDefault().GetSource() == "repo").ShouldBe(1, result);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    public async Task ShouldShowErrorWhenStaticRootHasSingletonOrScopedDependency(string lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency { }
                           
                               internal class Dependency : IDependency { }
                           
                               internal interface IService
                               {
                                   public IDependency Dependency1 { get; }
                                           
                                   public IDependency Dependency2 { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency1, Func<IDependency> dependency2)
                                   {
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2();
                                   }
                           
                                   public IDependency Dependency1 { get; }
                                           
                                   public IDependency Dependency2 { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<IDependency>().As(Lifetime.#lifetime#).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IDependency>("Dependency")
                                           .Root<IService>("Root", kind: RootKinds.Static);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service1 = Composition.Root;
                                       var service2 = Composition.Root;
                                       Console.WriteLine(service1.Dependency1 == service1.Dependency2);        
                                       Console.WriteLine(service2.Dependency1 == service1.Dependency1);
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime).RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Logs.Count(i => i.Id == LogId.ErrorLifetimeDefect).ShouldBe(1, result);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    public async Task ShouldShowErrorWhenStaticRootHasSingletonOrScopedFactoryDependency(string lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency
                               {
                                   bool IsInitialized { get; }
                               }
                               
                               class Dependency : IDependency
                               {
                                   public bool IsInitialized { get; private set; }
                               
                                   public void Initialize() => IsInitialized = true;
                               }
                               
                               interface IService
                               {
                                   IDependency Dependency { get; }
                               }
                               
                               class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                       Dependency = dependency;
                                   }
                                   public IDependency Dependency { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                       .Bind<IDependency>().As(Lifetime.#lifetime#).To<IDependency>(ctx =>
                                       {
                                           // Some logic for creating an instance:
                                           ctx.Inject(out Dependency dependency);
                                           dependency.Initialize();
                                           return dependency;
                                       })
                                       .Bind<IService>().To<Service>()
                                       
                                       // Composition root
                                       .Root<IService>("MyService", kind: RootKinds.Static);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = Composition.MyService;
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime).RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Logs.Count(i => i.Id == LogId.ErrorLifetimeDefect).ShouldBe(1, result);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    public async Task ShouldShowErrorWhenStaticRootHasSingletonOrScopedShortDependency(string lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency : IDependency {}
                               
                               interface IService {}
                               
                               class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.#lifetime#).To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<IService>("MyRoot", kind: RootKinds.Static);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var root = Composition.MyRoot;
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime).RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Logs.Count(i => i.Id == LogId.ErrorLifetimeDefect).ShouldBe(1, result);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    public async Task ShouldShowErrorWhenStaticRootHasSingletonOrScopedRootDependency(string lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency : IDependency {}
                               
                               interface IService {}
                               
                               class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Dependency>()
                                           .Bind().As(Lifetime.#lifetime#).To<Service>()
                                           .Root<IService>("MyRoot", kind: RootKinds.Static);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var root = Composition.MyRoot;
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime).RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Logs.Count(i => i.Id == LogId.ErrorLifetimeDefect).ShouldBe(1, result);
    }
}