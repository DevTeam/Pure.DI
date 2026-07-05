namespace Pure.DI.IntegrationTests;

using Core;

public class BindAttributeTests
{
    [Fact]
    public async Task ShouldUseReadablePersistentVariableNamesForSpecialBindings()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Type(typeof(IService))]
                               [Lifetime(Lifetime.Singleton)]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.ShouldContain($"private global::Sample.Service? _singletonService{Names.Salt};");
        result.GeneratedCode.ShouldNotContain("_singletonService214748");
    }

    [Fact]
    public async Task ShouldSupportCustomAttributeOnImplementationType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                               class BindingAttribute<T> : Attribute
                               {
                                   public BindingAttribute(Lifetime lifetime = Lifetime.Transient, object? tag = null) {}
                               }

                               interface IService {}

                               [Binding<IService>(Lifetime.Singleton, "main")]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .TypeAttribute<BindingAttribute<TT>>()
                                           .LifetimeAttribute<BindingAttribute<TT>>()
                                           .TagAttribute<BindingAttribute<TT>>(1)
                                           .Root<IService>("Root", "main");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportCustomAttributeDefaultLifetimeOnImplementationType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                               class BindingAttribute<T> : Attribute
                               {
                                   public BindingAttribute(Lifetime lifetime = Lifetime.Singleton) {}
                               }

                               interface IService {}

                               [Binding<IService>]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .TypeAttribute<BindingAttribute<TT>>()
                                           .LifetimeAttribute<BindingAttribute<TT>>()
                                           .Root<IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeOnImplementationType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind(typeof(IService), Lifetime.Singleton, "main")]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root", "main");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldWarnWhenSeveralImplementationTypesHaveSameBindAttributeContract()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind(typeof(IService), Lifetime.Transient, "main")]
                               class Service1 : IService {}

                               [Bind(typeof(IService), Lifetime.Transient, "main")]
                               class Service2 : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root", "main");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.StdOut.ShouldBe(["Service1"], result);
        result.Warnings.Count.ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding && i.Locations.FirstOrDefault().GetSource() == "Setup(\"Composition\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldSupportBindAttributeOnImplementationTypeWhenContractIsNotDefined()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Service"], result);
    }

    [Fact]
    public async Task ShouldSupportSeparateMetadataAttributesOnImplementationType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Type(typeof(IService))]
                               [Lifetime(Lifetime.Singleton)]
                               [Tag("main")]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root", "main");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldMergeSeveralBindAttributesIntoSingleBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind(typeof(IService)), Bind(typeof(IService)), Tag("main"), Tag("secondary"), Lifetime(Lifetime.Singleton)]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Main", "main")
                                           .Root<IService>("Secondary", "secondary");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Main, composition.Secondary));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldCreateSeparateBindingsForSeveralBindAttributeGroups()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind(typeof(IService), Lifetime.Singleton, "main")]
                               [Bind(typeof(IService), Lifetime.Singleton, "secondary")]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Main", "main")
                                           .Root<IService>("Secondary", "secondary");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Main, composition.Secondary));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["False"], result);
    }

    [Fact]
    public async Task ShouldMergeBindAttributeWithSeparateMetadataAttributes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind]
                               [Type(typeof(IService))]
                               [Tag("main")]
                               [Lifetime(Lifetime.Singleton)]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root", "main");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldFailWhenImplementationTypeHasRepeatedSameLifetime()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind(typeof(IService), Lifetime.Singleton, "main")]
                               [Lifetime(Lifetime.Singleton)]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root", "main");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(error => error.Id == LogId.ErrorInvalidBinding).ShouldBe(1);
    }

    [Fact]
    public async Task ShouldApplyTagsAndLifetimeToSimplifiedBindAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind]
                               [Tag("main")]
                               [Lifetime(Lifetime.Singleton)]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root", "main");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDeduplicateContractsAndTagsOnImplementationType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind(typeof(IService), Lifetime.Singleton, "main", "main"), Type(typeof(IService)), Tag("main")]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root", "main");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldMergeBindingAttributesFromPartialImplementationDeclarations()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind(typeof(IService), Lifetime.Singleton, "main")]
                               partial class Service : IService {}

                               [Tag("secondary")]
                               partial class Service {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Main", "main")
                                           .Root<IService>("Secondary", "secondary");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Main, composition.Secondary));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportNamedConstructorArgumentsForBindAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind(type: typeof(IService), lifetime: Lifetime.Singleton, tags: new object[] { "main" })]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root", "main");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Root, composition.Root));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldMergeCustomAndBuiltInBindingAttributesOnImplementationType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                               class BindingAttribute<T> : Attribute
                               {
                                   public BindingAttribute(object? tag) {}
                               }

                               interface IService {}

                               [Binding<IService>("main")]
                               [Tag("secondary")]
                               [Lifetime(Lifetime.Singleton)]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .TypeAttribute<BindingAttribute<TT>>()
                                           .TagAttribute<BindingAttribute<TT>>()
                                           .Root<IService>("Main", "main")
                                           .Root<IService>("Secondary", "secondary");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(ReferenceEquals(composition.Main, composition.Secondary));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericContractWithMarkerOnGenericImplementationType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<T>
                               {
                                   T Content { get; }
                               }

                               interface ICat {}

                               [Bind(typeof(IBox<TT>))]
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content)
                                   {
                                       Content = content;
                                   }

                                   public T Content { get; }
                               }

                               class Cat : ICat {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<ICat>().To<Cat>()
                                           .Root<IBox<ICat>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                       Console.WriteLine(composition.Root.Content.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["CardboardBox`1", "Cat"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericContractWithMarkerOnGenericRecordImplementationType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<out T>
                               {
                                   T Content { get; }
                               }

                               interface ICat {}

                               [Bind(typeof(IBox<TT>))]
                               record CardboardBox<T>(T Content) : IBox<T>;

                               class Cat : ICat {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<ICat>().To<Cat>()
                                           .Root<IBox<ICat>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Name);
                                       Console.WriteLine(composition.Root.Content.GetType().Name);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["CardboardBox`1", "Cat"], result);
    }

    [Fact]
    public async Task ShouldInferGenericImplementationMappingFromImplementedContract()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IMap<TKey, TValue>
                               {
                                   TKey Key { get; }

                                   TValue Value { get; }
                               }

                               [Bind(typeof(IMap<TT, TT1>))]
                               class PairBox<TValue, TKey> : IMap<TKey, TValue>
                               {
                                   public PairBox(TValue value, TKey key)
                                   {
                                       Value = value;
                                       Key = key;
                                   }

                                   public TKey Key { get; }

                                   public TValue Value { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<int>().To(_ => 7)
                                           .Bind<string>().To(_ => "abc")
                                           .Root<IMap<int, string>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var root = new Composition().Root;
                                       Console.WriteLine(root.GetType().Name);
                                       Console.WriteLine(root.Key);
                                       Console.WriteLine(root.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["PairBox`2", "7", "abc"], result);
    }

    [Fact]
    public async Task ShouldFailWhenImplementationTypeHasDifferentLifetimes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind(typeof(IService), Lifetime.Singleton, "main")]
                               [Lifetime(Lifetime.PerBlock)]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root", "main");
                                   }
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
        result.Errors.Count(error => error.Id == LogId.ErrorInvalidBinding).ShouldBe(1);
    }

    [Fact]
    public async Task ShouldFailWhenImplementationTypeHasRepeatedLifetimesInSameAttributeGroup()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}

                               [Bind(typeof(IService), Lifetime.Singleton, "main"), Lifetime(Lifetime.Singleton)]
                               class Service : IService {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<IService>("Root", "main");
                                   }
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
        result.Errors.Count(error => error.Id == LogId.ErrorInvalidBinding).ShouldBe(1);
    }
}
