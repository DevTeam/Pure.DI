namespace Pure.DI.IntegrationTests;

using Core;

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
                .Hint(Hint.Resolve, "Off")
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
        result.StdOut.ShouldBe(["Sample.CardboardBox`1[System.Int32]"], result);
    }
    
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
        result.Warnings.Count(i => i.Id == LogId.WarningTypeArgInResolveMethod).ShouldBe(1);
        result.StdOut.ShouldBe(["Sample.CardboardBox`1[System.Int32]"], result);
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
        void Setup()
        {
            DI.Setup(nameof(Composition))
                .Hint(Hint.Resolve, "Off")
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
        [
            "Sample.Service`2[System.Int32,System.DateTime]",
            "Sample.OtherService`1[System.String]"
        ], result);
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
        void Setup()
        {
            DI.Setup(nameof(Composition))
                .Hint(Hint.Resolve, "Off")
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
        result.StdOut.ShouldBe(["Sample.Consumer`1[System.Int32]"], result);
    }
    
    [Fact]
    public async Task ShouldSupportGenericRootWhenTypeConstraint()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace Sample
{
    class Disposable: IDisposable
    {
        public void Dispose()
        {
        }
    }
    
    class Dep<T>
        where T: IDisposable
    {
    }

    interface IBox<T, TStruct>
        where T: IDisposable
        where TStruct: struct 
    { 
        T? Content { get; set; } 
    }

    class CardboardBox<T, TStruct> : IBox<T, TStruct>
        where T: IDisposable
        where TStruct: struct
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
                .Hint(Hint.Resolve, "Off")
                .Bind<IBox<TTDisposable, TTS>>().To<CardboardBox<TTDisposable, TTS>>()                
                // Composition Root
                .Root<IBox<TTDisposable, TTS>>("GetRoot");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();            
            var root = composition.GetRoot<Disposable, int>();
            Console.WriteLine(root);
        }
    }                
}
""".RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.CardboardBox`2[Sample.Disposable,System.Int32]"], result);
    }
    
    [Fact]
    public async Task ShouldSupportGenericRootWhenStructConstraint()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace Sample
{
    interface IDependency<T> { }

    class Dependency<T> : IDependency<T> { }

    interface IService<T, TStruct>
        where TStruct: struct
    {
    }

    class Service<T, TStruct>: IService<T, TStruct>
        where TStruct: struct
    {
        public Service(IDependency<T> dependency)
        {
        }
    }

    class OtherService<T, TStruct>: IService<T, TStruct>
        where TStruct: struct
    {
        public OtherService(IDependency<T> dependency)
        {
        }
    }
    
    internal partial class Composition
    {
        void Setup()
        {
            DI.Setup(nameof(Composition))
                .Hint(Hint.Resolve, "Off")
                .Bind().To<Dependency<TT>>()
                .Bind().To<Service<TT, TTS>>()
                .Bind("Other").To(ctx =>
                {
                    ctx.Inject(out IDependency<TT> dependency);
                    return new OtherService<TT, TTS>(dependency);
                })
                .Root<IService<TT, TTS>>("GetMyRoot")
                .Root<IService<TT, TTS>>("GetOtherService", "Other");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.GetMyRoot<int, double>();
            Console.WriteLine(service);
        }
    }                
}
""".RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service`2[System.Int32,System.Double]"], result);
    }
    
    [Fact]
    public async Task ShouldSupportGenericRootWhenArray()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using System.Collections.Generic;
using static Pure.DI.Lifetime;

namespace Sample
{
    interface IRecipient<T>
    {
    }

    interface IPost<T>
    {
    }

    class Post<T> : IPost<T>
    {
        public Post(IRecipient<T>[] recipients)
        {
        Console.WriteLine(recipients.Length);
        }
    }
    
    class Recipient<TT> : IRecipient<TT>
    {
    }

    internal partial class Composition
    {
        void Setup()
        {
            DI.Setup(nameof(Composition))
                .Hint(Hint.Resolve, "Off")
                .Bind<IRecipient<TT>>(1).To<Recipient<TT>>()
                .Bind<IRecipient<TT>>(2).To<Recipient<TT>>()
                .RootBind<IPost<TT>>("GetRoot").To<Post<TT>>();
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();            
            var root = composition.GetRoot<string>();
            Console.WriteLine(root);
        }
    }                
}
""".RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2", "Sample.Post`1[System.String]"], result);
    }
    
    [Theory]
    [InlineData("System.Collections.Generic.IEnumerable")]
    [InlineData("System.Collections.Generic.IList")]
    [InlineData("System.Collections.Immutable.ImmutableArray")]
    [InlineData("System.Collections.Generic.IReadOnlyList")]
    [InlineData("System.Collections.Generic.ISet")]
    [InlineData("System.Collections.Generic.HashSet")]
    [InlineData("System.Collections.Generic.Queue")]
    [InlineData("System.Collections.Generic.Stack")]
    [InlineData("System.Collections.Immutable.IImmutableList")]
    [InlineData("System.Collections.Immutable.ImmutableList")]
    [InlineData("System.Collections.Immutable.IImmutableSet")]
    [InlineData("System.Collections.Immutable.IImmutableQueue")]
    [InlineData("System.Collections.Immutable.ImmutableQueue")]
    [InlineData("System.Collections.Immutable.IImmutableStack")]
    [InlineData("System.Collections.Immutable.ImmutableStack")]
    [InlineData("System.Span", "Length")]
    [InlineData("System.Span", "Length", LanguageVersion.CSharp8)]
    [InlineData("System.ReadOnlySpan", "Length")]
    [InlineData("System.Memory", "Length")]
    [InlineData("System.ReadOnlyMemory", "Length")]
    [InlineData("System.Collections.Concurrent.IProducerConsumerCollection")]
    [InlineData("System.Collections.Concurrent.ConcurrentBag")]
    [InlineData("System.Collections.Concurrent.ConcurrentQueue")]
    [InlineData("System.Collections.Concurrent.ConcurrentStack")]
    [InlineData("System.Collections.Concurrent.BlockingCollection")]
    public async Task ShouldSupportGenericRootWhenCollection(string collectionType, string counter = "Count()", LanguageVersion languageVersion = LanguageVersion.CSharp9)
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using System.Collections.Generic;
using System.Linq;
using static Pure.DI.Lifetime;

namespace Sample
{
    interface IRecipient<T>
    {
    }

    interface IPost<T>
    {
    }

    class Post<T> : IPost<T>
    {
        public Post(###CollectionType###<IRecipient<T>> recipients)
        {
            Console.WriteLine(recipients.###Counter###);
        }
    }
    
    class Recipient<TT> : IRecipient<TT>
    {
    }

    internal partial class Composition
    {
        void Setup()
        {
            DI.Setup(nameof(Composition))
                .Hint(Hint.Resolve, "Off")
                .Bind<IRecipient<TT>>(1).To<Recipient<TT>>()
                .Bind<IRecipient<TT>>(2).To<Recipient<TT>>()
                .RootBind<IPost<TT>>("GetRoot").To<Post<TT>>();
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();            
            var root = composition.GetRoot<string>();
        }
    }                
}
"""
            .Replace("###CollectionType###", collectionType)
            .Replace("###Counter###", counter)
            .RunAsync(
            new Options
            {
                LanguageVersion = languageVersion,
                NullableContextOptions = NullableContextOptions.Disable
            });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }
}