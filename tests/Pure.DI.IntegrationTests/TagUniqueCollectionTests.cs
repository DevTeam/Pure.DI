namespace Pure.DI.IntegrationTests;

/// <summary>
/// Pins the aggregation behavior of <see cref="Tag.Unique"/> for the most
/// common collection types. These tests replace the hypothesis held in the
/// previous AI.Client chat
/// (commit <c>168b9a2</c> in <c>C:\Projects\DevTeam\AI.Client\src\AI.Contracts\Composition.cs</c>):
/// the old conclusion was that <c>Tag.Unique</c> only aggregates into
/// <c>IEnumerable&lt;T&gt;</c>, which forced the manual-array workaround.
/// Empirically (see the assertions below) Pure.DI aggregates into all three
/// of <c>IEnumerable&lt;T&gt;</c>, <c>IReadOnlyCollection&lt;T&gt;</c> and
/// <c>ICollection&lt;T&gt;</c>, so the test suite documents the real contract
/// and prevents silent regressions in either direction.
/// </summary>
public class TagUniqueCollectionTests
{
    [Fact]
    public async Task ShouldAggregateUniqueBindingsInFactoryFromInternalSetup()
    {
        // Two namespace blocks in one file must retain their own setup names.
        // Singleton<GenericAdapter>() also contributes its IAdapter contract to the collection.
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample.Contracts
                           {
                               internal interface IAdapter { }
                               internal sealed class GenericAdapter : IAdapter { }
                               internal sealed class FileAdapter : IAdapter { }
                               internal sealed class ProcessAdapter : IAdapter { }

                               internal sealed class Presentations
                               {
                                   public Presentations(GenericAdapter generic, IReadOnlyCollection<IAdapter> adapters)
                                   {
                                       foreach (var adapter in adapters)
                                       {
                                           Console.WriteLine(adapter.GetType().Name);
                                       }
                                   }
                               }

                               internal sealed class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(kind: CompositionKind.Internal)
                                           .Bind<Presentations>().As(Lifetime.Singleton)
                                               .To((GenericAdapter generic, IReadOnlyCollection<IAdapter> adapters)
                                                   => new Presentations(generic, adapters))
                                           .Singleton<GenericAdapter>()
                                           .Bind<IAdapter>(Tag.Unique).To<FileAdapter>()
                                           .Bind<IAdapter>(Tag.Unique).To<ProcessAdapter>();
                               }
                           }

                           namespace Sample
                           {
                               internal sealed partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup()
                                           .DependsOn("Sample.Contracts.Composition")
                                           .Root<Sample.Contracts.Presentations>("Root");
                               }

                               public static class Program
                               {
                                   public static void Main() => _ = new Composition().Root;
                               }
                           }
                           """.RunAsync();

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["GenericAdapter", "FileAdapter", "ProcessAdapter"], result);
    }

    [Fact]
    public async Task ShouldAggregateIntoIEnumerable()
    {
        // Given

        // When
        var result = await """
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample
                           {
                               internal interface IDep { }

                               internal class Dep1 : IDep { }

                               internal class Dep2 : IDep { }

                               internal interface IService { }

                               internal class Service : IService
                               {
                                   public Service(IEnumerable<IDep> deps)
                                   {
                                       System.Console.WriteLine(deps.Count());
                                   }
                               }

                               internal partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup("Composition")
                                           .Bind<IDep>(Unique).To<Dep1>()
                                           .Bind(Unique).To<Dep2>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.ToString();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldAggregateIntoIReadOnlyCollection()
    {
        // Given
        // This contradicts the hypothesis from the previous AI.Client chat, which
        // claimed that Tag.Unique does not aggregate into IReadOnlyCollection<T>.
        // The empirical result below (count == 2) is the actual contract.

        // When
        var result = await """
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample
                           {
                               internal interface IDep { }

                               internal class Dep1 : IDep { }

                               internal class Dep2 : IDep { }

                               internal interface IService { }

                               internal class Service : IService
                               {
                                   public Service(IReadOnlyCollection<IDep> deps)
                                   {
                                       System.Console.WriteLine(deps.Count);
                                   }
                               }

                               internal partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup("Composition")
                                           .Bind<IDep>(Unique).To<Dep1>()
                                           .Bind(Unique).To<Dep2>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.ToString();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldAggregateIntoICollection()
    {
        // Given
        // Same as the IReadOnlyCollection case: aggregation works.

        // When
        var result = await """
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample
                           {
                               internal interface IDep { }

                               internal class Dep1 : IDep { }

                               internal class Dep2 : IDep { }

                               internal interface IService { }

                               internal class Service : IService
                               {
                                   public Service(ICollection<IDep> deps)
                                   {
                                       System.Console.WriteLine(deps.Count);
                                   }
                               }

                               internal partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup("Composition")
                                           .Bind<IDep>(Unique).To<Dep1>()
                                           .Bind(Unique).To<Dep2>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.ToString();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldAggregateIntoIReadOnlyList()
    {
        // Given
        // IReadOnlyList<T> sits between IReadOnlyCollection<T> and IList<T>.
        // Pins the contract for the same reason as the IReadOnlyCollection case.

        // When
        var result = await """
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample
                           {
                               internal interface IDep { }

                               internal class Dep1 : IDep { }

                               internal class Dep2 : IDep { }

                               internal interface IService { }

                               internal class Service : IService
                               {
                                   public Service(IReadOnlyList<IDep> deps)
                                   {
                                       System.Console.WriteLine(deps.Count);
                                   }
                               }

                               internal partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup("Composition")
                                           .Bind<IDep>(Unique).To<Dep1>()
                                           .Bind(Unique).To<Dep2>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.ToString();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldAggregateIntoIList()
    {
        // Given
        // IList<T> is the mutable counterpart of IReadOnlyList<T>.
        // Pins the contract for the same reason as the ICollection case.

        // When
        var result = await """
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample
                           {
                               internal interface IDep { }

                               internal class Dep1 : IDep { }

                               internal class Dep2 : IDep { }

                               internal interface IService { }

                               internal class Service : IService
                               {
                                   public Service(IList<IDep> deps)
                                   {
                                       System.Console.WriteLine(deps.Count);
                                   }
                               }

                               internal partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup("Composition")
                                           .Bind<IDep>(Unique).To<Dep1>()
                                           .Bind(Unique).To<Dep2>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.ToString();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldAggregateIntoArray()
    {
        // Given
        // T[] is the most concrete collection form. Pins the contract for the
        // most demanding consumer signature.

        // When
        var result = await """
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample
                           {
                               internal interface IDep { }

                               internal class Dep1 : IDep { }

                               internal class Dep2 : IDep { }

                               internal interface IService { }

                               internal class Service : IService
                               {
                                   public Service(IDep[] deps)
                                   {
                                       System.Console.WriteLine(deps.Length);
                                   }
                               }

                               internal partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup("Composition")
                                           .Bind<IDep>(Unique).To<Dep1>()
                                           .Bind(Unique).To<Dep2>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.ToString();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldAggregateWhenUsingExplicitArrayBinding()
    {
        // Given
        // The "explicit parameters + manual array" pattern used by commit
        // 168b9a2 in AI.Contracts/Composition.cs. Even though aggregation into
        // IReadOnlyCollection<T> actually works (see the test above), the
        // explicit-array pattern remains a valid escape hatch when a consumer
        // cannot rely on Tag.Unique aggregation (for example because the
        // consumer signature is IReadOnlyList<T> or another collection type).

        // When
        var result = await """
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           namespace Sample
                           {
                               internal interface IDep { }

                               internal class Dep1 : IDep { }

                               internal class Dep2 : IDep { }

                               internal interface IService { }

                               internal class Service : IService
                               {
                                   public Service(IReadOnlyList<IDep> deps)
                                   {
                                       System.Console.WriteLine(deps.Count);
                                   }
                               }

                               internal partial class Composition
                               {
                                   void Setup() =>
                                       DI.Setup("Composition")
                                           .Bind<IDep>(Unique).To<Dep1>()
                                           .Bind(Unique).To<Dep2>()
                                           .Bind<IReadOnlyList<IDep>>().To(ctx =>
                                           {
                                               ctx.Inject<IEnumerable<IDep>>(out var deps);
                                               return deps.ToArray();
                                           })
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.ToString();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }
}
