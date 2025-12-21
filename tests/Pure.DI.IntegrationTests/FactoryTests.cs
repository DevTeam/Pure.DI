namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the factory bindings using lambdas and their customization.
/// </summary>
public class FactoryTests
{
    [Theory]
    [InlineData("123", "ctx.Tag", "123")]
    [InlineData("\"123\"", "ctx.Tag", "123")]
    [InlineData("'1'", "((char)ctx.Tag) + 1", "50")]
    [InlineData("123", "(object)ctx.Tag", "123")]
    [InlineData("123", "int.Parse(ctx.Tag.ToString() ?? \"\")", "123")]
    [InlineData("123", "dependency.GetInt((int)ctx.Tag)", "123")]
    [InlineData("123", "((int)ctx.Tag) * 2", "246")]
    [InlineData("123", "dependency.GetInt((int)ctx.Tag) * 2", "246")]
    [InlineData("typeof(int)", "ctx.Tag", "System.Int32")]
    public async Task ShouldSupportFactoryWhenUsingParentTag(string tag, string tagExpression, string output)
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
                     int GetInt(int val);
                }
            
                class Dependency: IDependency
                {
                     public int GetInt(int val) => val;
                }
            
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
                            .Bind<IDependency>(#tag#).To(ctx => new Dependency())
                            .Bind<IService>(#tag#).To(ctx => {
                                ctx.Inject<IDependency>(ctx.Tag, out var dependency);
                                Console.WriteLine(#tagExpression#);
                                return new Service(dependency);
                            })
                            .Root<IService>("Service", #tag#);
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
            """
            .Replace("#tagExpression#", tagExpression)
            .Replace("#tag#", tag)
            .RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([output, "Created"], result);
    }

    [Theory]
    [InlineData(nameof(Lifetime.Transient), "Consumer1, Root, Composition", "Consumer2, Root, Composition", "Root, Composition", "Composition")]
    [InlineData(nameof(Lifetime.Singleton), "Consumer1, Root, Composition", "Consumer2, Root, Composition", "Root, Composition", "Composition")]
    [InlineData(nameof(Lifetime.Scoped), "Consumer1, Root, Composition", "Consumer2, Root, Composition", "Root, Composition", "Composition")]
    [InlineData(nameof(Lifetime.PerBlock), "Consumer1, Root, Composition", "Consumer2, Root, Composition", "Root, Composition", "Composition")]
    [InlineData(nameof(Lifetime.PerResolve), "Consumer1, Root, Composition", "Consumer2, Root, Composition", "Root, Composition", "Composition")]
    public async Task ShouldSupportFactoryWhenUsingConsumers(string lifetime, params string[] consumers)
    {
        // Given

        // When
        var result = await """
            using System;
            using System.Linq;
            using Pure.DI;

            namespace Sample
            {
                public class Log
                {
                    private readonly Type[] _types;
                
                    public Log(Type[] types)
                    {
                        _types = types;
                    }
                
                    public void Info()
                    {
                        Console.WriteLine(string.Join(", ", _types.Select(t => t.Name)));
                    }
                }
                
                public class Consumer1
                {
                    public Consumer1(Log log)
                    {
                        log.Info();
                    }
                }
                
                public class Consumer2
                {
                    public Consumer2(Log log)
                    {
                        log.Info();
                    }
                }
                
                public class Root
                {
                    public Root(Log log, Consumer1 consumer1, Consumer2 consumer2)
                    {
                        log.Info();
                    }
                }
            
                public partial class Composition
                {
                    void Setup() => DI.Setup()
                        .Bind().To(ctx => new Log(ctx.ConsumerTypes))
                        .Bind().As(Lifetime.#lifetime#).To<Consumer1>()
                        .Root<Root>("Root")
                        .Root<Log>("Log");
                }
            
                public class Program
                {
                    public static void Main()
                    {
                        var composition = new Composition();
                        var root = composition.Root;
                        var log = composition.Log;
                        log.Info();
                    }
                }
            }
            """
            .Replace("#lifetime#", lifetime)
            .RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(consumers.AsEnumerable(), result);
    }

    [Theory]
    [InlineData(nameof(Lifetime.Transient), "Consumer1", "Consumer2", "Root", "Composition")]
    [InlineData(nameof(Lifetime.Singleton), "Consumer1", "Consumer2", "Root", "Composition")]
    [InlineData(nameof(Lifetime.Scoped), "Consumer1", "Consumer2", "Root", "Composition")]
    [InlineData(nameof(Lifetime.PerBlock), "Consumer1", "Consumer2", "Root", "Composition")]
    [InlineData(nameof(Lifetime.PerResolve), "Consumer1", "Consumer2", "Root", "Composition")]
    public async Task ShouldSupportFactoryWhenUsingConsumer(string lifetime, params string[] consumers)
    {
        // Given

        // When
        var result = await """
            using System;
            using System.Linq;
            using Pure.DI;

            namespace Sample
            {
                public class Log
                {
                    private readonly Type _type;
                
                    public Log(Type type)
                    {
                        _type = type;
                    }
                
                    public void Info()
                    {
                        Console.WriteLine(_type.Name);
                    }
                }
                
                public class Consumer1
                {
                    public Consumer1(Log log)
                    {
                        log.Info();
                    }
                }
                
                public class Consumer2
                {
                    public Consumer2(Log log)
                    {
                        log.Info();
                    }
                }
                
                public class Root
                {
                    public Root(Log log, Consumer1 consumer1, Consumer2 consumer2)
                    {
                        log.Info();
                    }
                }
            
                public partial class Composition
                {
                    void Setup() => DI.Setup()
                        .Bind().To(ctx => new Log(ctx.ConsumerType))
                        .Bind().As(Lifetime.#lifetime#).To<Consumer1>()
                        .Root<Root>("Root")
                        .Root<Log>("Log");
                }
            
                public class Program
                {
                    public static void Main()
                    {
                        var composition = new Composition();
                        var root = composition.Root;
                        var log = composition.Log;
                        log.Info();
                    }
                }
            }
            """
            .Replace("#lifetime#", lifetime)
            .RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(consumers.AsEnumerable(), result);
    }

    [Theory]
    [InlineData(Lifetime.Transient)]
    [InlineData(Lifetime.PerBlock)]
    [InlineData(Lifetime.PerResolve)]
    [InlineData(Lifetime.Scoped)]
    [InlineData(Lifetime.Singleton)]
    internal async Task ShouldSupportBuildUpWhenLifetime(Lifetime lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class ClientScript
                               {
                                   [Ordinal(1)] public IService1? service1;
                                   
                                   [Ordinal(2)] public IService2? service2;
                               }
                               
                               interface IService1
                               {
                               };
                               
                               internal class Service1 : IService1
                               {
                               };
                               
                               interface IService2
                               {
                               };
                               
                               internal class Service2 : IService2
                               {
                               };
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Service1>()
                                           .Bind().As(Lifetime.#lifetime#).To<Service2>()
                                           .Bind().To<ClientScript>(ctx =>
                                           {
                                               ctx.Inject("from arg", out ClientScript script);
                                               ctx.BuildUp(script);
                                               return script;
                                           })
                                           .RootArg<ClientScript>("clientScript", "from arg")
                                           .Root<ClientScript>("Initialize");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var script = new ClientScript();
                                       script = composition.Initialize(script);
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime.ToString()).RunAsync();

        // Then
        result.Errors.Count.ShouldBe(0, result);
    }

    [Fact]
    public async Task ShouldReplaceContextTag()
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
                                           .Bind<IDependency>(123).To(ctx => new Dependency())
                                           .Bind<IService>(123).To(ctx => {
                                               System.Console.WriteLine(ctx.Tag);
                                               ctx.Inject<IDependency>(ctx.Tag, out var dependency);
                                               return new Service(dependency);
                                           })
                                           .Root<IService>("Service", 123);
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
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["123", "Created"], result);
    }

    [Fact]
    public async Task ShouldReplaceContextTagWhenTagIsNull()
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
                                           .Bind<IDependency>(123).To(ctx => new Dependency())
                                           .Bind<IService>().To(ctx => {
                                               System.Console.WriteLine(ctx.Tag ?? "null");
                                               ctx.Inject<IDependency>(123, out var dependency);
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
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["null", "Created"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildUpWhenGeneric()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                           
                               class Dependency<T>: IDependency<T>
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] T depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency<string> Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency<string> dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public IDependency<string> Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind<IDependency<string>>().To(ctx => {
                                               var dep = new Dependency<string>();
                                               ctx.BuildUp(dep);
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
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildUpWhenMethod()
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
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind<IDependency>().To(ctx => {
                                               var dep = new Dependency();
                                               ctx.BuildUp(dep);
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
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildUpWhenMethodAndField()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                                   
                                   [Ordinal(2)]
                                   public int Id;
                               }
                           
                               interface IService
                               {
                                   Dependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(Dependency dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public Dependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To(_ => 33)
                                           .Bind().To(ctx => {
                                               var dep = new Dependency();
                                               ctx.BuildUp(dep);
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
                                       Console.WriteLine($"Id: {service.Dep.Id}");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc", "Id: 33"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildUpWhenMethodAndProperty()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                                   
                                   [Ordinal(2)]
                                   public int Id { get; set; }
                               }
                           
                               interface IService
                               {
                                   Dependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(Dependency dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public Dependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To(_ => 33)
                                           .Bind().To(ctx => {
                                               var dep = new Dependency();
                                               ctx.BuildUp(dep);
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
                                       Console.WriteLine($"Id: {service.Dep.Id}");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc", "Id: 33"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildUpWhenMethodAndPropertyInIf()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Dependency
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                                   
                                   [Ordinal(2)]
                                   public int Id { get; set; }
                               }
                           
                               interface IService
                               {
                                   Dependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(Dependency dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public Dependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To(_ => 33)
                                           .Bind().To(ctx => {
                                               var dep = new Dependency();
                                               if (true)
                                               {
                                                    ctx.BuildUp(dep);
                                               }
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
                                       Console.WriteLine($"Id: {service.Dep.Id}");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc", "Id: 33"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildUpWhenOpenGeneric()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                           
                               class Dependency<T>: IDependency<T>
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] T depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency<string> Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency<string> dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public IDependency<string> Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To(ctx => {
                                               var dep = new Dependency<TT>();
                                               ctx.BuildUp(dep);
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
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportBuildUpWhenRoot()
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
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().To<Dependency>()
                                           .RootArg<Service>("service", "arg")
                                           .Bind().To<Service>(ctx => 
                                           {
                                               ctx.Inject("arg", out Service service);
                                               ctx.BuildUp(service);
                                               return service;
                                           })
                                           .Root<IService>("BuildUpService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.BuildUpService(new Service());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc"], result);
    }
    [Fact]
    public async Task ShouldSupportFactory()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To(ctx =>
                                                // My Comment 
                                                new Dependency())
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("// My Comment").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWhenBlock()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // FormatCode = On
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To(ctx => 
                                               { 
                                                   // My comment
                                                   return new Dependency(); 
                                               }
                                           )
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.Contains("// My comment").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWhenExplicitImplementation()
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
                               void Init();
                           }

                           class Dependency : IDependency
                           {
                               void IDependency.Init()
                               {
                               }
                           }

                           interface IService
                           {
                           }

                           class Service : IService
                           {
                               public Service(IDependency dependency)
                               {
                               }
                           }

                           partial class Composition
                           {
                               private void Setup() =>
                                   DI.Setup(nameof(Composition))
                                       .Hint(Hint.OnNewInstance, "On")
                                       .Hint(Hint.OnDependencyInjection, "On")
                                       .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                       .Bind<IService>().To(ctx =>
                                       {
                                           ctx.Inject(out IDependency dependency);
                                           dependency.Init();
                                           return new Service(dependency);
                                       })
                                       .Root<IService>("MyService");
                           
                               partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime)
                               {
                                   System.Console.WriteLine(typeof(T));
                               }
                           
                               private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
                               {
                                   System.Console.WriteLine(typeof(T));
                                   return value;
                               }
                           }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = new Composition().MyService;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency", "Sample.IDependency", "Sample.Service", "Sample.IService"], result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWhenGenericParenthesis()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<IDependency>((ctx) => new Dependency())
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWhenMethodInjection()
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
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Arg<string>("depName", 374)
                                           .Bind<IDependency>().To(ctx => {
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
                                       var composition = new Composition("dep");
                                       var service = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize dep"], result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWhenParenthesis()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To((ctx) => new Dependency())
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWhenSingleton()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To(ctx => new Dependency())
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
                                       Console.WriteLine(service1.Dep == service2.Dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWithInject()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To(ctx => new Dependency())
                                           .Bind<IService>().To(ctx => {
                                               IDependency @dependency1;
                                               ctx.Inject<IDependency>(out @dependency1); new Service(@dependency1); ctx.Inject<IDependency>(out var dependency2); return new Service(dependency2);
                                           })
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWithInjectInFunc()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To(ctx => new Dependency())
                                           .Bind<IService>().To(ctx => {
                                               var func = new Func<IDependency>(() => {
                                                   ctx.Inject<IDependency>(out var dependency);
                                                   return dependency;
                                               });
                                               
                                               var dependency2 = func();
                                               return new Service(dependency2);
                                           })   
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWithInjectWhenBlocks()
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
                                  public Dependency(int id, string str)
                                  {
                                  }
                              }
                           
                              interface IService
                              {
                                  IDependency Dep { get; }
                              }
                           
                              class Service: IService
                              {
                                  public Service(IDependency dep)
                                  {
                                      Dep = dep;
                                  }
                           
                                  public IDependency Dep { get; }
                              }
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      DI.Setup("Composition")
                                          .Bind<string>().To(_ => "Abc")
                                          .Bind<int>().To(_ => 33)
                                          .Bind<IDependency>(22).To<Dependency>()
                                          .Bind<IDependency>().To(ctx => new Dependency(0, "xyz"))
                                          .Bind<IService>().To(ctx => {
                                              IDependency dependency1;
                                              var rnd = new Random(1).Next(3);
                                              if (rnd == 0)
                                                  ctx.Inject(22, out dependency1);
                                              else
                                                  if (rnd == 2)
                                                  {
                                                      ctx.Inject(22, out dependency1);
                                                  }
                                                  else
                                                  {
                                                      if (rnd == 1)
                                                      {
                                                           ctx.Inject(out dependency1);
                                                           return new Service(dependency1);
                                                      }
                                                      
                                                      ctx.Inject(out dependency1);
                                                  }
                                              
                                              return new Service(dependency1);
                                          })
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWithInjectWhenGenericTypeIsNotSpecifiedAsTypeArgument()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To(ctx => new Dependency())
                                           .Bind<IService>().To(ctx => {
                                               IDependency dependency1;
                                               ctx.Inject(out dependency1); new Service(dependency1); ctx.Inject(out IDependency dependency2); return new Service(dependency2);
                                           })
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWithInjectWithTheSameValueName()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               class MyRoot
                               {
                                   public MyRoot(IService service1, [Tag(2)] IService service2) { }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To(ctx => new Dependency())
                                           .Bind<IService>().To(ctx => {
                                               ctx.Inject<IDependency>(out var dependency);
                                               return new Service(dependency);
                                           })
                                           .Bind<IService>(2).To(ctx => {
                                               IDependency dependency;
                                               ctx.Inject<IDependency>(out dependency);
                                               return new Service(dependency);
                                           })
                                           .Root<MyRoot>("Root");
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
    public async Task ShouldSupportFactoryWithParentTagInjection()
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
                                           .Bind<IDependency>(123).To(ctx => new Dependency())
                                           .Bind<IService>(123).To(ctx => {
                                               ctx.Inject<IDependency>(ctx.Tag, out var dependency);
                                               return new Service(dependency);
                                           })
                                           .Root<IService>("Service", 123);
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
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Created"], result);
    }

    [Fact]
    public async Task ShouldSupportFactoryWithSimpleInject()
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
                                  }
                           
                                  public IDependency Dep { get; }
                              }
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      DI.Setup("Composition")
                                          .Bind<IDependency>().To(ctx => new Dependency())
                                          .Bind<IService>().To(ctx => {
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
                                      var service1 = composition.Service;
                                      var service2 = composition.Service;
                                      Console.WriteLine(service1.Dep != service2.Dep);
                                  }
                              }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportInjectionWhenOpenGeneric()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                           
                               class Dependency<T>: IDependency<T>
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] T depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency<string> Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency<string> dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public IDependency<string> Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind(1).To<Dependency<TT>>()
                                           .Bind().To(ctx => {
                                               ctx.Inject(1, out IDependency<TT> dep);
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
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportInjectWithSmartTag()
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
                                  }
                           
                                  public IDependency Dep { get; }
                              }
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      DI.Setup("Composition")
                                          .Bind<IDependency>(Tag.MyTag).To(ctx => new Dependency())
                                          .Bind<IService>().To(ctx => {
                                              ctx.Inject<IDependency>(Tag.MyTag, out var dependency);
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
                                      var service1 = composition.Service;
                                      var service2 = composition.Service;
                                      Console.WriteLine(service1.Dep != service2.Dep);
                                  }
                              }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportInjectWithSmartTagWhenItTheSameAsSomeTypeName()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Tag;
                           
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
                                  public Service([Tag(Injection)]IDependency dep)
                                  {
                                      Dep = dep;
                                  }
                           
                                  public IDependency Dep { get; }
                              }
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      DI.Setup("Composition")
                                          .Bind<IDependency>(Injection).To(ctx => new Dependency())
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportInjectWithTag()
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
                                  }
                           
                                  public IDependency Dep { get; }
                              }
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      DI.Setup("Composition")
                                          .Bind<IDependency>("my tag").To(ctx => new Dependency())
                                          .Bind<IService>().To(ctx => {
                                              ctx.Inject<IDependency>("my tag", out var dependency);
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
                                      var service1 = composition.Service;
                                      var service2 = composition.Service;
                                      Console.WriteLine(service1.Dep != service2.Dep);
                                  }
                              }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonWithinFactoryAndFunc()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency2 {}
                           
                               class Dependency2: IDependency2 { }
                               
                               interface IDependency3 {}
                           
                               class Dependency3
                               {
                                   public Dependency3(IDependency2 depFactory)
                                   {
                                   }
                               }
                           
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(IDependency2 depFactory)
                                   {
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService
                               {
                                   private Func<IDependency> _depFactory;
                                   public Service(Func<IDependency> depFactory, Dependency3 dep3)
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
                                           .Bind<IDependency2>().As(Lifetime.Singleton).To<Dependency2>()
                                           .Bind<IDependency>().To(ctx =>
                                           {
                                                ctx.Inject<IDependency2>(out var dep2);
                                                return new Dependency(dep2);
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
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains(" = new global::Sample.Dependency2();")).ShouldBe(1, result);
    }

    [Theory]
    [InlineData("PerResolve")]
    [InlineData("Singleton")]
    [InlineData("Transient")]
    [InlineData("PerBlock")]
    public async Task ShouldSupportTheSameFactoryVarNames(string lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                                using Pure.DI;
                           
                                public class Dependency : IDependency
                                {
                                    public Dependency(int val) { }
                                }
                           
                                public interface IDependency
                                {
                                }
                           
                                public class Dependency2 : IDependency2
                                {
                                    public Dependency2(IDependency dependency, string str) { }
                                }
                           
                                public interface IDependency2
                                {
                                }
                           
                                public interface IService
                                {
                                }
                           
                                public class Service : IService
                                {
                                    public Service(IDependency dependency, Func<string, IDependency2> factory)
                                    {
                                    }
                                }
                           
                                public partial class Composition
                                {
                                    public static void Setup() =>
                                        // FormatCode = On
                                        DI.Setup(nameof(Composition))
                                            .Bind<int>().To(_ => {
                                                var Console = 33;
                                                return Console; 
                                             })
                                            .Bind<IDependency>().As(Lifetime.###).To(ctx => {
                                               var Console = 44;
                                               System.Console.WriteLine(Console);
                                               ctx.Inject<int>(out var dep2);
                                               return new Dependency(dep2);
                                           })
                                            .Bind<Func<string, IDependency2>>().As(Lifetime.###).To(ctx => new Func<string, IDependency2>(p => {
                                                ctx.Inject<IDependency>(out var dep2);
                                                return new Dependency2(dep2, p);
                                            }))
                                            .Bind<IService>().To<Service>()
                                            .Root<IService>("Service");
                                }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       IService service = composition.Service;
                                   }
                               }
                           }
                           """.Replace("###", lifetime).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportGenericBuildUpWhenCustomGenericMarker()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public class Node { };
                           
                               public class Dependency { };
                               
                               public class Scene : Node
                               {
                                   [Dependency]
                                   public Dependency? Dependency { get; set; }
                               }
                               
                               public class Main
                               {
                                   [Dependency]
                                   public Accessor<Scene> Scene { get; set; }
                               }
                               
                               public static class Loader
                               {
                                   public static T Load<T>() => Activator.CreateInstance<T>();
                               }
                               
                               public struct Accessor<T>
                                   where T : Node
                               {
                                   public Accessor(T value)
                                   {
                                       Value = value;
                                   }
                               
                                   public T Value { get; }
                               }
                           
                               public partial class Composition
                               {
                                   static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Dependency>()
                                           .GenericTypeArgument<TTNode>()
                                           .Bind<Accessor<TTNode>>(Tag.Any)
                                               .As(Lifetime.PerResolve)
                                               .To(ctx =>
                                               {
                                                   var node = Loader.Load<TTNode>();
                                                   ctx.BuildUp(node);
                                                   return new Accessor<TTNode>(node);
                                               })
                                           .Root<Main>("Main");
                               
                                   class TTNode : Node { };
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var main = composition.Main;
                                       Console.WriteLine(main.Scene.Value.Dependency?.ToString() ?? "null");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency"], result);
    }

    [Fact]
    public async Task ShouldSupportCtxLock()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To(ctx => new Dependency())
                                           .Bind<IService>().To(ctx => {
                                               lock(ctx.Lock)
                                               {
                                                   ctx.Inject<IDependency>(out var dependency);
                                                   return new Service(dependency);
                                               }
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
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportCtxLockWhenStaticRoot()
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
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To(ctx => new Dependency())
                                           .Bind<IService>().To(ctx => {
                                               lock(ctx.Lock)
                                               {
                                                   ctx.Inject<IDependency>(out var dependency);
                                                   return new Service(dependency);
                                               }
                                           })
                                           .Root<IService>("Service", kind: RootKinds.Static);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var service = Composition.Service;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
}