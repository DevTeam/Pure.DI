namespace Pure.DI.IntegrationTests;

/// <summary>
/// A binding that injects a collection of its own contract must not receive itself as an element of that collection.
/// </summary>
public class CollectionSelfInclusionTests
{
    [Fact]
    public async Task ShouldNotIncludeCompositeInItsOwnEnumerableWhenOpenGenericBindingIsOverridden()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample;

                           interface IHandler<T> { string Name { get; } }

                           sealed class DefaultHandler<T> : IHandler<T> { public string Name => "Default"; }

                           sealed class HandlerA : IHandler<int> { public string Name => "A"; }

                           sealed class HandlerB : IHandler<int> { public string Name => "B"; }

                           sealed class CompositeIntHandler : IHandler<int>
                           {
                               private readonly IEnumerable<IHandler<int>> parts;

                               public CompositeIntHandler(IEnumerable<IHandler<int>> parts) => this.parts = parts;

                               public string Name => $"Composite({string.Join(",", parts.Select(i => i.Name))})";
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IHandler<TT>>().To<DefaultHandler<TT>>()
                                   .Bind<IHandler<int>>(Unique).To<HandlerA>()
                                   .Bind<IHandler<int>>(Unique).To<HandlerB>()
                                   .Bind<IHandler<int>>().To<CompositeIntHandler>()
                                   .Root<IHandler<string>>("StringHandler")
                                   .Root<IHandler<int>>("IntHandler");
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.StringHandler.Name);
                                   Console.WriteLine(composition.IntHandler.Name);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["Default", "Composite(A,B)"], result);
    }

    [Fact]
    public async Task ShouldNotIncludeCompositeInItsOwnEnumerableWhenItOverridesAnotherBinding()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample;

                           interface IService { string Name { get; } }

                           sealed class AService : IService { public string Name => "A"; }

                           sealed class BService : IService { public string Name => "B"; }

                           sealed class LeafService : IService { public string Name => "Leaf"; }

                           sealed class CompositeService : IService
                           {
                               private readonly IEnumerable<IService> services;

                               public CompositeService(IEnumerable<IService> services) => this.services = services;

                               public string Name => $"Composite({string.Join(",", services.Select(i => i.Name))})";
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IService>(Unique).To<AService>()
                                   .Bind<IService>(Unique).To<BService>()
                                   .Bind<IService>().To<LeafService>()
                                   .Bind<IService>().To<CompositeService>()
                                   .Root<IService>("Service");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.Name);
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["Composite(A,B)"], result);
    }

    [Fact]
    public async Task ShouldNotIncludeCompositeInItsOwnEnumerableWhenItEnumeratesInConstructor()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample;

                           interface IService { string Name { get; } }

                           sealed class AService : IService { public string Name => "A"; }

                           sealed class BService : IService { public string Name => "B"; }

                           sealed class LeafService : IService { public string Name => "Leaf"; }

                           sealed class CompositeService : IService
                           {
                               private readonly List<IService> _services;

                               public CompositeService(IEnumerable<IService> services) => _services = services.ToList();

                               public string Name => $"Composite({string.Join(",", _services.Select(i => i.Name))})";
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IService>(Unique).To<AService>()
                                   .Bind<IService>(Unique).To<BService>()
                                   .Bind<IService>().To<LeafService>()
                                   .Bind<IService>().To<CompositeService>()
                                   .Root<IService>("Service");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.Name);
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["Composite(A,B)"], result);
    }

    [Fact]
    public async Task ShouldNotIncludeCompositeInItsOwnArray()
    {
        var result = await """
                           using System;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample;

                           interface IService { string Name { get; } }

                           sealed class AService : IService { public string Name => "A"; }

                           sealed class BService : IService { public string Name => "B"; }

                           sealed class LeafService : IService { public string Name => "Leaf"; }

                           sealed class CompositeService : IService
                           {
                               private readonly IService[] services;

                               public CompositeService(IService[] services) => this.services = services;

                               public string Name => $"Composite({string.Join(",", services.Select(i => i.Name))})";
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IService>(Unique).To<AService>()
                                   .Bind<IService>(Unique).To<BService>()
                                   .Bind<IService>().To<LeafService>()
                                   .Bind<IService>().To<CompositeService>()
                                   .Root<IService>("Service");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.Name);
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["Composite(A,B)"], result);
    }

    [Fact]
    public async Task ShouldNotIncludeTaggedCompositeInItsOwnEnumerableWhenCollectionIsAlsoARoot()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample;

                           interface IService { string Name { get; } }

                           sealed class AService : IService { public string Name => "A"; }

                           sealed class BService : IService { public string Name => "B"; }

                           sealed class CompositeService : IService
                           {
                               private readonly IEnumerable<IService> services;

                               public CompositeService(IEnumerable<IService> services) => this.services = services;

                               public string Name => $"Composite({string.Join(",", services.Select(i => i.Name))})";
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IService>(Unique).To<AService>()
                                   .Bind<IService>(Unique).To<BService>()
                                   .Bind<IService>("composite").To<CompositeService>()
                                   .Root<IEnumerable<IService>>("Services")
                                   .Root<IService>("Composite", "composite");
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(string.Join(",", composition.Services.Select(i => i.Name)));
                                   Console.WriteLine(composition.Composite.Name);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["A,B,Composite(A,B)", "Composite(A,B)"], result);
    }

    [Fact]
    public async Task ShouldNotIncludeGenericCompositeInItsOwnEnumerable()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample;

                           interface IHandler<T> { string Name { get; } }

                           sealed class HandlerA<T> : IHandler<T> { public string Name => "A"; }

                           sealed class HandlerB<T> : IHandler<T> { public string Name => "B"; }

                           sealed class LeafHandler<T> : IHandler<T> { public string Name => "Leaf"; }

                           sealed class CompositeHandler<T> : IHandler<T>
                           {
                               private readonly IEnumerable<IHandler<T>> parts;

                               public CompositeHandler(IEnumerable<IHandler<T>> parts) => this.parts = parts;

                               public string Name => $"Composite({string.Join(",", parts.Select(i => i.Name))})";
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IHandler<TT>>(Unique).To<HandlerA<TT>>()
                                   .Bind<IHandler<TT>>(Unique).To<HandlerB<TT>>()
                                   .Bind<IHandler<TT>>().To<LeafHandler<TT>>()
                                   .Bind<IHandler<TT>>().To<CompositeHandler<TT>>()
                                   .Root<IHandler<int>>("IntHandler");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().IntHandler.Name);
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["Composite(A,B)"], result);
    }

    [Fact]
    public async Task ShouldKeepOverridingBindingWhenCompositeIsDeclaredFirst()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample;

                           interface IService { string Name { get; } }

                           sealed class AService : IService { public string Name => "A"; }

                           sealed class LeafService : IService { public string Name => "Leaf"; }

                           sealed class CompositeService : IService
                           {
                               private readonly IEnumerable<IService> services;

                               public CompositeService(IEnumerable<IService> services) => this.services = services;

                               public string Name => $"Composite({string.Join(",", services.Select(i => i.Name))})";
                           }

                           sealed class Consumer
                           {
                               private readonly CompositeService composite;

                               public Consumer(CompositeService composite) => this.composite = composite;

                               public string Name => composite.Name;
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IService>(Unique).To<AService>()
                                   .Bind<IService>().To<CompositeService>()
                                   .Bind<IService>().To<LeafService>()
                                   .Root<Consumer>("Consumer");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Consumer.Name);
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["Composite(A,Leaf)"], result);
    }

    // A cycle through a lazy collection stays allowed, as in issue #160: construction is finite,
    // and only the composite itself is excluded from its own collection.
    [Fact]
    public async Task ShouldAllowCycleThroughLazyCollectionWhenElementDependsOnComposite()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample;

                           interface IService { string Name { get; } }

                           sealed class AService : IService { public string Name => "A"; }

                           sealed class Wrapper : IService
                           {
                               private readonly IService inner;

                               public Wrapper(IService inner) => this.inner = inner;

                               public string Name => $"Wrapper({inner.GetType().Name})";
                           }

                           sealed class CompositeService : IService
                           {
                               private readonly IEnumerable<IService> services;

                               public CompositeService(IEnumerable<IService> services) => this.services = services;

                               public string Name => $"Composite({string.Join(",", services.Select(i => i.Name))})";
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IService>(Unique).To<AService>()
                                   .Bind<IService>(Unique).To<Wrapper>()
                                   .Bind<IService>().To<CompositeService>()
                                   .Root<IService>("Service");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.Name);
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["Composite(A,Wrapper(CompositeService))"], result);
    }
}
