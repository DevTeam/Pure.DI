// ReSharper disable StringLiteralTypo

namespace Pure.DI.IntegrationTests;

public class CtorTests
{
    [Fact]
    public async Task ShouldPreferCtorWithActualDependency()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<out T> { T Content { get; } }
                           
                               interface ICat { }
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content) => Content = content;
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               class ShroedingersCat : ICat
                               {
                                   public ShroedingersCat(int id = 99)
                                   {
                                       Console.WriteLine(id);
                                   }
                           
                                   internal ShroedingersCat(string name)
                                   {
                                       Console.WriteLine(name);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>().To(_ => "Murka")
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   private void Run() { }
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var program = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Murka"], result);
    }

    [Fact]
    public async Task ShouldPreferCtorWithOrdinalAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<out T> { T Content { get; } }
                           
                               interface ICat { }
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content) => Content = content;
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               class ShroedingersCat : ICat
                               {        
                                   public ShroedingersCat(string name, int id = 33)
                                   {
                                       Console.WriteLine(name);
                                   }
                           
                                   [Ordinal(1)]
                                   public ShroedingersCat(string name, int id = 33, int id2 = 34)
                                   {
                                       Console.WriteLine(name);
                                   }
                           
                                   [Ordinal(0)]
                                   public ShroedingersCat(int id = 99)
                                   {
                                       Console.WriteLine(id);
                                   }
                           
                                   internal ShroedingersCat(string name)
                                   {
                                       Console.WriteLine(name);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>().To(_ => "Murka")
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   private void Run() { }
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var program = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(1, result);
        result.StdOut.ShouldBe(["99"], result);
    }

    [Fact]
    public async Task ShouldSelectDefaultCtorWhenHasStatic()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<out T> { T Content { get; } }
                           
                               interface ICat { }
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content) => Content = content;
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               class ShroedingersCat : ICat
                               {
                                   static ShroedingersCat() { }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   private void Run() => Console.WriteLine(_box);
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSelectValidCtor()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<out T> { T Content { get; } }
                           
                               interface ICat { }
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content) => Content = content;
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               class ShroedingersCat : ICat
                               {
                                   public ShroedingersCat(int id) { }
                           
                                   internal ShroedingersCat() { }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   private void Run() => Console.WriteLine(_box);
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportUseParameterlessCtor()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency
                               {
                                   ValueTask DoSomething(CancellationToken cancellationToken);
                               }
                               
                               class Dependency : IDependency
                               {
                                   public ValueTask DoSomething(CancellationToken cancellationToken) => ValueTask.CompletedTask;
                               }
                               
                               interface IService
                               {
                                   Task RunAsync(CancellationToken cancellationToken);
                               }
                               
                               class Service : IService
                               {
                                   private readonly Task<IDependency> _dependencyTask;
                               
                                   public Service(Task<IDependency> dependencyTask)
                                   {
                                       _dependencyTask = dependencyTask;
                                       _dependencyTask.Start();
                                   }
                               
                                   public async Task RunAsync(CancellationToken cancellationToken)
                                   {
                                       var dependency = await _dependencyTask;
                                       await dependency.DoSomething(cancellationToken);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.Resolve, "Off")
                                           .Bind<Task<TT>>().To(ctx =>
                                           {
                                               ctx.Inject(ctx.Tag, out Func<TT> factory);
                                               ctx.Inject(out CancellationToken cancellationToken);
                                               return new Task<TT>(factory, cancellationToken);
                                           })
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>().Root<IService>("Root")
                                           .Bind<CancellationTokenSource>().As(Lifetime.Singleton).To<CancellationTokenSource>()
                                           // Specifies to use CancellationToken from the composition root argument,
                                           // if not specified then CancellationToken.None will be used
                                           .Bind<CancellationToken>().To(ctx =>
                                           {
                                               ctx.Inject(out CancellationTokenSource cancellationTokenSource);
                                               return cancellationTokenSource.Token;
                                           });
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
    public async Task ShouldUseCtorItHasDefaultValue()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<out T> { T Content { get; } }
                           
                               interface ICat { }
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content) => Content = content;
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               class ShroedingersCat : ICat
                               {
                                   public ShroedingersCat(int id = 99)
                                   {
                                       Console.WriteLine(id);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   private void Run() { }
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var program = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["99"], result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldSupportRefCtorArgs()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Service
                               {
                                   [Ordinal]
                                   public void Initialize(ref Indexes indexes) =>
                                       indexes.Print();
                               }
                               
                               readonly ref struct Indexes
                               {
                                   private readonly ref int[] _dep;
                                   
                                   public Indexes(ref int[] dep)
                                   {
                                       _dep = ref dep;
                                   }
                               
                                   public void Print()
                                   {
                                       foreach (var i in _dep)
                                       {
                                           Console.WriteLine(i);
                                       }
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<int[]>(_ => new int[]{1, 2, 3})
                                           .Root<Service>("MyService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.MyService;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Latest));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2", "3"], result);
    }
#endif
}