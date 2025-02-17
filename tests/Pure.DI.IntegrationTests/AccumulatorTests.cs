﻿namespace Pure.DI.IntegrationTests;

public class AccumulatorTests
{
    [Fact]
    public async Task ShouldSupportAccumulator()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IAccumulating
                               {
                               }
                           
                               class MyAccumulator : List<IAccumulating>
                               {
                               }
                           
                               interface IDependency
                               {
                               }
                           
                               class AbcDependency : IDependency, IAccumulating
                               {
                               }
                           
                               class XyzDependency : IDependency, IAccumulating
                               {
                               }
                           
                               interface IService: IAccumulating
                               {
                                   IDependency Dependency1 { get; }
                           
                                   IDependency Dependency2 { get; }
                                   
                                   IDependency Dependency3 { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service([Tag(typeof(AbcDependency))] IDependency dependency1,
                                       [Tag(typeof(XyzDependency))] IDependency dependency2,
                                       IDependency dependency3)
                                   {
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                       Dependency3 = dependency3;
                                   }
                           
                                   public IDependency Dependency1 { get; }
                           
                                   public IDependency Dependency2 { get; }
                           
                                   public IDependency Dependency3 { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Accumulate<IAccumulating, MyAccumulator>(Lifetime.Transient, Lifetime.Singleton)
                                           .Bind<IDependency>().As(Lifetime.PerBlock).To<AbcDependency>()
                                           .Bind<IDependency>(Tag.Type).To<AbcDependency>()
                                           .Bind<IDependency>(Tag.Type).As(Lifetime.Singleton).To<XyzDependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<(IService service, MyAccumulator accumulator)>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var (service, accumulator) = composition.Root;
                                       Console.WriteLine(accumulator.Count);
                                       foreach(var val in accumulator)
                                       {
                                           Console.WriteLine(val);
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["3", "Sample.XyzDependency", "Sample.AbcDependency", "Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportAccumulatorWhenDifferentLifetimes()
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
                                   bool IsDisposed { get; }
                               }
                               
                               class Dependency : IDependency, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                               
                                   public void Dispose() => IsDisposed = true;
                               }
                               
                               interface IService
                               {
                                   public IDependency Dependency { get; }
                                   
                                   public IDependency SingleDependency { get; }
                               }
                               
                               class Service : IService, IDisposable
                               {
                                   private readonly Owned<IDependency> _dependency;
                                   private readonly Owned<IDependency> _singleDependency;
                               
                                   public Service(
                                       Func<Owned<IDependency>> dependencyFactory,
                                       [Tag("single")] Owned<IDependency> singleDependency)
                                   {
                                       _singleDependency = singleDependency;
                                       _dependency = dependencyFactory();
                                   }
                               
                                   public IDependency Dependency => _dependency.Value;
                               
                                   public IDependency SingleDependency => _singleDependency.Value;
                               
                                   public void Dispose()
                                   {
                                       _dependency.Dispose();
                                       _singleDependency.Dispose();
                                   }
                               }
                               
                               partial class Composition
                               {
                                   static void Setup() =>
                                       DI.Setup()
                                           .Bind().To<Dependency>()
                                           .Bind("single").As(Lifetime.Singleton).To<Dependency>()
                                           .Bind().To<Service>()
                               
                                           // Composition root
                                           .Root<Service>("Root");
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root1 = composition.Root;
                                       var root2 = composition.Root;
                                       Console.WriteLine(root1.Dependency != root2.Dependency);
                                       Console.WriteLine(root1.SingleDependency == root2.SingleDependency);
                                       
                                       root2.Dispose();
                                       
                                       // Checks that the disposable instances
                                       // associated with root1 have been disposed of
                                       Console.WriteLine(root2.Dependency.IsDisposed);
                                       
                                       // But the singleton is still not disposed of
                                       Console.WriteLine(root2.SingleDependency.IsDisposed == false);
                                       
                                       // Checks that the disposable instances
                                       // associated with root2 have not been disposed of
                                       Console.WriteLine(root1.Dependency.IsDisposed == false);
                                       Console.WriteLine(root1.SingleDependency.IsDisposed == false);
                                       
                                       root1.Dispose();
                                       
                                       // Checks that the disposable instances
                                       // associated with root2 have been disposed of
                                       Console.WriteLine(root1.Dependency.IsDisposed);
                                       
                                       // But the singleton is still not disposed of
                                       Console.WriteLine(root1.SingleDependency.IsDisposed == false);
                                       
                                       composition.Dispose();
                                       Console.WriteLine(root1.SingleDependency.IsDisposed);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportAccumulatorWhenDifferentLifetimes2()
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
                                   bool IsDisposed { get; }
                               }
                               
                               class Dependency : IDependency, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                               
                                   public void Dispose() => IsDisposed = true;
                               }
                               
                               interface IService
                               {
                                   public IDependency Dependency { get; }
                                   
                                   public IDependency SingleDependency { get; }
                               }
                               
                               class Service : IService, IDisposable
                               {
                                   private readonly Owned<IDependency> _dependency;
                                   private readonly Owned<IDependency> _singleDependency;
                               
                                   public Service(
                                       Func<Owned<IDependency>> dependencyFactory,
                                       [Tag("single")] Func<Owned<IDependency>> singleDependencyFactory)
                                   {
                                       _singleDependency = singleDependencyFactory();
                                       _dependency = dependencyFactory();
                                   }
                               
                                   public IDependency Dependency => _dependency.Value;
                               
                                   public IDependency SingleDependency => _singleDependency.Value;
                               
                                   public void Dispose()
                                   {
                                       _dependency.Dispose();
                                       _singleDependency.Dispose();
                                   }
                               }
                               
                               partial class Composition
                               {
                                   static void Setup() =>
                                       DI.Setup()
                                           .Bind().To<Dependency>()
                                           .Bind("single").As(Lifetime.Singleton).To<Dependency>()
                                           .Bind().To<Service>()
                               
                                           // Composition root
                                           .Root<Service>("Root");
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root1 = composition.Root;
                                       var root2 = composition.Root;
                                       Console.WriteLine(root1.Dependency != root2.Dependency);
                                       Console.WriteLine(root1.SingleDependency == root2.SingleDependency);
                                       
                                       root2.Dispose();
                                       
                                       // Checks that the disposable instances
                                       // associated with root1 have been disposed of
                                       Console.WriteLine(root2.Dependency.IsDisposed);
                                       
                                       // But the singleton is still not disposed of
                                       Console.WriteLine(root2.SingleDependency.IsDisposed == false);
                                       
                                       // Checks that the disposable instances
                                       // associated with root2 have not been disposed of
                                       Console.WriteLine(root1.Dependency.IsDisposed == false);
                                       Console.WriteLine(root1.SingleDependency.IsDisposed == false);
                                       
                                       root1.Dispose();
                                       
                                       // Checks that the disposable instances
                                       // associated with root2 have been disposed of
                                       Console.WriteLine(root1.Dependency.IsDisposed);
                                       
                                       // But the singleton is still not disposed of
                                       Console.WriteLine(root1.SingleDependency.IsDisposed == false);
                                       
                                       composition.Dispose();
                                       Console.WriteLine(root1.SingleDependency.IsDisposed);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportAccumulatorWhenForContracts()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IAccumulating
                               {
                               }
                               
                               interface IAccumulating2
                               {
                               }
                           
                               class MyAccumulator : List<object>
                               {
                               }
                           
                               interface IDependency
                               {
                               }
                           
                               class AbcDependency : IDependency, IAccumulating
                               {
                               }
                           
                               class XyzDependency : IDependency, IAccumulating2
                               {
                               }
                           
                               interface IService: IAccumulating
                               {
                                   IDependency Dependency1 { get; }
                           
                                   IDependency Dependency2 { get; }
                                   
                                   IDependency Dependency3 { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service([Tag(typeof(AbcDependency))] IDependency dependency1,
                                       [Tag(typeof(XyzDependency))] IDependency dependency2,
                                       IDependency dependency3)
                                   {
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                       Dependency3 = dependency3;
                                   }
                           
                                   public IDependency Dependency1 { get; }
                           
                                   public IDependency Dependency2 { get; }
                           
                                   public IDependency Dependency3 { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Accumulate<IAccumulating, MyAccumulator>(Lifetime.Transient, Lifetime.Singleton)
                                           .Accumulate<IAccumulating2, MyAccumulator>(Lifetime.Transient, Lifetime.Singleton)
                                           .Bind<IDependency>().As(Lifetime.PerBlock).To<AbcDependency>()
                                           .Bind<IDependency>(Tag.Type).To<AbcDependency>()
                                           .Bind<IDependency>(Tag.Type).As(Lifetime.Singleton).To<XyzDependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<(IService service, MyAccumulator accumulator)>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var (service, accumulator) = composition.Root;
                                       Console.WriteLine(accumulator.Count);
                                       foreach(var val in accumulator)
                                       {
                                           Console.WriteLine(val);
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["3", "Sample.XyzDependency", "Sample.AbcDependency", "Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportAccumulatorWhenLifetime()
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
                                   public Service(IDependency dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                               
                               class MyAccumulator: List<object> { }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.PerResolve).To(ctx => new Dependency())
                                           .Bind<IService>().To<Service>()
                                           .Accumulate<object, MyAccumulator>(Lifetime.Transient, Lifetime.PerBlock)
                                           .Root<(IService service, MyAccumulator acc)>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Service;
                                       var service = root.service;
                                       root.acc.ForEach(i => Console.WriteLine(i));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service", "(Sample.Service, Sample.MyAccumulator)"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericOwned()
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
                                       bool IsDisposed { get; }
                                   }
                           
                                   class Dependency : IDependency, IDisposable
                                   {
                                       public bool IsDisposed { get; private set; }
                           
                                       public void Dispose() => IsDisposed = true;
                                   }
                           
                                   interface IService
                                   {
                                       public IDependency Dependency { get; }
                                   }
                           
                                   class Service : IService
                                   {
                                       public Service(IDependency dependency)
                                       {
                                           Dependency = dependency;
                                       }
                                       
                                       public IDependency Dependency { get; }
                                   }
                           
                                   partial class Composition
                                   {
                                       void Setup() =>
                                           DI.Setup(nameof(Composition))
                                               .Bind<IDependency>().To<Dependency>()
                                               .Bind<IService>().To<Service>()
                                               .Root<Owned<IService>>("Root");
                                   }
                           
                                   public class Program
                                   {
                                       public static void Main()
                                       {
                                           var composition = new Composition();
                                           var root1 = composition.Root;
                                           var root2 = composition.Root;
                                           root2.Dispose();
                                           Console.WriteLine(root2.Value.Dependency.IsDisposed);
                                           Console.WriteLine(root1.Value.Dependency.IsDisposed);
                                           root1.Dispose();
                                           Console.WriteLine(root1.Value.Dependency.IsDisposed);
                                       }
                                   }
                               }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportIOwned()
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
                                       bool IsDisposed { get; }
                                   }
                           
                                   class Dependency : IDependency, IDisposable
                                   {
                                       public bool IsDisposed { get; private set; }
                           
                                       public void Dispose() => IsDisposed = true;
                                   }
                           
                                   interface IService
                                   {
                                       public IDependency Dependency { get; }
                                   }
                           
                                   class Service : IService
                                   {
                                       public Service(IDependency dependency)
                                       {
                                           Dependency = dependency;
                                       }
                                       
                                       public IDependency Dependency { get; }
                                   }
                           
                                   partial class Composition
                                   {
                                       void Setup() =>
                                           DI.Setup(nameof(Composition))
                                               .Bind<IDependency>().To<Dependency>()
                                               .Bind<IService>().To<Service>()
                                               .Root<(IService Service, IOwned Owned)>("Root");
                                   }
                           
                                   public class Program
                                   {
                                       public static void Main()
                                       {
                                           var composition = new Composition();
                                           var root1 = composition.Root;
                                           var root2 = composition.Root;
                                           root2.Owned.Dispose();
                                           Console.WriteLine(root2.Service.Dependency.IsDisposed);
                                           Console.WriteLine(root1.Service.Dependency.IsDisposed);
                                           root1.Owned.Dispose();
                                           Console.WriteLine(root1.Service.Dependency.IsDisposed);
                                       }
                                   }
                               }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False", "True"], result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldSupportMultipleLifetimesAsArray()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IAccumulating
                               {
                               }
                           
                               class MyAccumulator : List<IAccumulating>
                               {
                               }
                           
                               interface IDependency
                               {
                               }
                           
                               class AbcDependency : IDependency, IAccumulating
                               {
                               }
                           
                               class XyzDependency : IDependency, IAccumulating
                               {
                               }
                           
                               interface IService: IAccumulating
                               {
                                   IDependency Dependency1 { get; }
                           
                                   IDependency Dependency2 { get; }
                                   
                                   IDependency Dependency3 { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service([Tag(typeof(AbcDependency))] IDependency dependency1,
                                       [Tag(typeof(XyzDependency))] IDependency dependency2,
                                       IDependency dependency3)
                                   {
                                       Dependency1 = dependency1;
                                       Dependency2 = dependency2;
                                       Dependency3 = dependency3;
                                   }
                           
                                   public IDependency Dependency1 { get; }
                           
                                   public IDependency Dependency2 { get; }
                           
                                   public IDependency Dependency3 { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Accumulate<IAccumulating, MyAccumulator>([Lifetime.Transient, Lifetime.Singleton])
                                           .Bind<IDependency>().As(Lifetime.PerBlock).To<AbcDependency>()
                                           .Bind<IDependency>(Tag.Type).To<AbcDependency>()
                                           .Bind<IDependency>(Tag.Type).As(Lifetime.Singleton).To<XyzDependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<(IService service, MyAccumulator accumulator)>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var (service, accumulator) = composition.Root;
                                       Console.WriteLine(accumulator.Count);
                                       foreach(var val in accumulator)
                                       {
                                           Console.WriteLine(val);
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp12 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["3", "Sample.XyzDependency", "Sample.AbcDependency", "Sample.Service"]);
    }
#endif

    [Fact]
    public async Task ShouldSupportOwned()
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
                                       bool IsDisposed { get; }
                                   }
                           
                                   class Dependency : IDependency, IDisposable
                                   {
                                       public bool IsDisposed { get; private set; }
                           
                                       public void Dispose() => IsDisposed = true;
                                   }
                           
                                   interface IService
                                   {
                                       public IDependency Dependency { get; }
                                   }
                           
                                   class Service : IService
                                   {
                                       public Service(IDependency dependency)
                                       {
                                           Dependency = dependency;
                                       }
                                       
                                       public IDependency Dependency { get; }
                                   }
                           
                                   partial class Composition
                                   {
                                       void Setup() =>
                                           DI.Setup(nameof(Composition))
                                               .Bind<IDependency>().To<Dependency>()
                                               .Bind<IService>().To<Service>()
                                               .Root<(IService Service, Owned Owned)>("Root");
                                   }
                           
                                   public class Program
                                   {
                                       public static void Main()
                                       {
                                           var composition = new Composition();
                                           var root1 = composition.Root;
                                           var root2 = composition.Root;
                                           root2.Owned.Dispose();
                                           Console.WriteLine(root2.Service.Dependency.IsDisposed);
                                           Console.WriteLine(root1.Service.Dependency.IsDisposed);
                                           root1.Owned.Dispose();
                                           Console.WriteLine(root1.Service.Dependency.IsDisposed);
                                       }
                                   }
                               }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False", "True"], result);
    }

    [Fact]
    public async Task ShroedingersCatScenarioWhenAccumulator()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               // Let's create an abstraction
                           
                               interface IBox<out T> { T Content { get; } }
                           
                               enum State { Alive, Dead }
                           
                               interface ICat { State State { get; } }
                           
                               // Here is our implementation
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(Func<(T value, Accumulator acc)> contentFactory)
                                   {
                                       var content = contentFactory();
                                       foreach(var dep in content.acc.Items)
                                       {
                                           Console.WriteLine(dep);
                                       }
                                       
                                       Console.WriteLine("CardboardBox created");
                                       Content = content.value;
                                   }
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               class ShroedingersCat : ICat
                               {
                                   // Represents the superposition of the states
                                   private readonly Lazy<State> _superposition;
                           
                                   public ShroedingersCat(Lazy<State> superposition) => _superposition = superposition;
                           
                                   // The decoherence of the superposition at the time of observation via an irreversible process
                                   public State State => _superposition.Value;
                               }
                               
                               class Accumulator 
                               {
                                   private readonly List<object> _items = new List<object>();
                                   
                                   public IEnumerable<object> Items =>_items.ToArray();
                                   
                                   public void Add(object item) => _items.Add(item);
                               }
                           
                               // Let's glue all together
                           
                               internal partial class Composition
                               {
                                   void Setup()
                                   {
                                       // FormatCode = On
                                       DI.Setup(nameof(Composition))
                                           .Accumulate<object, Accumulator>(Transient)
                                           .Accumulate<object, Accumulator>(Singleton)
                                           // Models a random subatomic event that may or may not occur
                                           .Bind<Random>().As(Singleton).To<Random>()
                                           // Represents a quantum superposition of 2 states: Alive or Dead
                                           .Bind<State>().To(ctx =>
                                           {
                                               ctx.Inject<Random>(out var random);
                                               return (State)random.Next(2);
                                           }) 
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           // Represents a cardboard box with any content
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           // Composition Root
                                           .Root<(Program program, Accumulator acc)>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root);
                                       foreach(var dep in root.acc.Items)
                                       {
                                           Console.WriteLine(dep);
                                       }
                           
                                       Console.WriteLine("Program created");
                                   }
                               }
                           }
                           """.RunAsync(new Options
        {
            LanguageVersion = LanguageVersion.CSharp8,
            NullableContextOptions = NullableContextOptions.Disable
        });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Value is not created.", "Sample.ShroedingersCat", "(Sample.ShroedingersCat, Sample.Accumulator)", "CardboardBox created", "(Sample.Program, Sample.Accumulator)", "[Sample.ShroedingersCat]", "Sample.Program", "(Sample.Program, Sample.Accumulator)", "Program created"], result);
    }
}