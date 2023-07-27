// ReSharper disable StringLiteralTypo
namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class CtorTests
{
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
        result.StdOut.ShouldBe(ImmutableArray.Create("99"), result);
    }
    
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
        result.StdOut.ShouldBe(ImmutableArray.Create("Murka"), result);
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("99"), result);
    }
}