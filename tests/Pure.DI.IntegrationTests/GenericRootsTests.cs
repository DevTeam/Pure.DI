namespace Pure.DI.IntegrationTests;

public class GenericRootsTests
{
    [Fact]
    public async Task ShouldSupportGenericRoot()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace Sample
{
    interface IBox<T> { T? Content { get; set; } }

    class CardboardBox<T> : IBox<T>
    {
        public T? Content { get; set; }
    }

    internal partial class Composition
    {
        private static void Setup()
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.CardboardBox`1[System.Int32]"), result);
    }
    
    [Fact]
    public async Task ShouldSupportGenericRootWhenFewTypeArgs()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace Sample
{
    interface IDependency<T, TVal> { }

    class Dependency<T, TVal> : IDependency<T, TVal> { }

    interface IService<TVal, T> { }

    class Service<TVal, T> : IService<TVal, T>
    {
        public Service(IDependency<T,TVal> dependency) { }
    }

    class OtherService<T> : IService<bool, T>
    {
        public OtherService(IDependency<T, double> dependency) { }
    }

    internal partial class Composition
    {
        private static void Setup()
        {
            DI.Setup(nameof(Composition))
                .Bind().To<Dependency<TT, TT1>>()
                .Bind().To<Service<TT, TT1>>()
                .Root<IService<TT, TT1>>("GetMyRoot")
                .Bind("Other").To(ctx =>
                {
                    ctx.Inject(out IDependency<TT, double> dependency);
                    return new OtherService<TT>(dependency);
                })
                .Root<IService<bool, TT>>("GetOtherService", "Other");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.GetMyRoot<int, DateTime>();
            Console.WriteLine(service);
            var someOtherService = composition.GetOtherService<string>();
            Console.WriteLine(someOtherService);
        }
    }                
}
""".RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(
            ImmutableArray.Create(
                "Sample.Service`2[System.Int32,System.DateTime]",
                "Sample.OtherService`1[System.String]"), result);
    }
    
    [Fact]
    public async Task ShouldSupportGenericRootWhenFactory()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace Sample
{
    interface IBox<T> { T? Content { get; set; } }

    class CardboardBox<T> : IBox<T>
    {
        public T? Content { get; set; }
    }
    
    class Consumer<T>
    {
        public Consumer(IBox<T> box)
        {
        }
    }

    internal partial class Composition
    {
        private static void Setup()
        {
            DI.Setup(nameof(Composition))
                .Bind<IBox<TT>>().To<CardboardBox<TT>>()
                .Bind<Consumer<TT>>().To<Consumer<TT>>(ctx => {
                    ctx.Inject<IBox<TT>>(out var box);
                    return new Consumer<TT>(box);
                })
                // Composition Root
                .Root<Consumer<TT>>("GetRoot");
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.Consumer`1[System.Int32]"), result);
    }
}