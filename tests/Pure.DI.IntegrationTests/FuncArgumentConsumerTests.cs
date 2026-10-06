namespace Pure.DI.IntegrationTests;

/// <summary>
/// Instances that consume, or sit next to consumers of, the argument of a <c>Func&lt;TArg, T&gt;</c>, see #157.
/// </summary>
public class FuncArgumentConsumerTests
{
    [Theory]
    [InlineData("Transient", "Func<Settings, Top>", "create(new Settings(name))", "two two two")]
    [InlineData("PerBlock", "Func<Settings, Top>", "create(new Settings(name))", "two two two")]
    [InlineData("PerBlock", "Func<Settings, Func<Top>>", "create(new Settings(name))()", "two two two")]
    [InlineData("PerBlock", "Func<Settings, Owned<Top>>", "create(new Settings(name)).Value", "two two two")]
    // A PerResolve instance is created once per resolve of the root, which is the Func itself, so later calls keep the first call's instances.
    [InlineData("PerResolve", "Func<Settings, Top>", "create(new Settings(name))", "one one one")]
    [InlineData("PerResolve", "Func<Settings, Func<Top>>", "create(new Settings(name))()", "one one one")]
    public async Task ShouldPassEachCallsArgumentToItsConsumers(string lifetime, string rootType, string call, string secondCall)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var create = composition.Create;
                                       foreach (var name in new[] { "one", "two" })
                                       {
                                           Top top = #call#;
                                           Console.WriteLine($"{top.A.Leaf.Settings.Name} {top.B.Leaf.Settings.Name} {top.Leaf.Settings.Name}");
                                       }
                                   }
                               }

                               sealed class Settings
                               {
                                   public Settings(string name) => Name = name;

                                   public string Name { get; }
                               }

                               sealed class Leaf
                               {
                                   public Leaf(Settings settings) => Settings = settings;

                                   public Settings Settings { get; }
                               }

                               sealed class Mid
                               {
                                   public Mid(Leaf leaf) => Leaf = leaf;

                                   public Leaf Leaf { get; }
                               }

                               sealed class Top
                               {
                                   public Top(Mid a, Mid b, Leaf leaf)
                                   {
                                       A = a;
                                       B = b;
                                       Leaf = leaf;
                                   }

                                   public Mid A { get; }

                                   public Mid B { get; }

                                   public Leaf Leaf { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<Leaf>().As(#lifetime#).To<Leaf>()
                                           .Bind<Mid>().As(#lifetime#).To<Mid>()
                                           .Root<#rootType#>("Create");
                               }
                           }
                           """.Replace("#lifetime#", lifetime).Replace("#rootType#", rootType).Replace("#call#", call).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["one one one", secondCall], result);
    }

    [Fact]
    public async Task ShouldCreateSingletonInOnePlaceNextToArgumentConsumer()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Top first = composition.Create(new Settings("one"));
                                       Top second = composition.Create(new Settings("two"));
                                       Console.WriteLine($"{first.A.Settings.Name} {first.B.Settings.Name} {second.A.Settings.Name} {second.B.Settings.Name}");
                                       Console.WriteLine(ReferenceEquals(first.SharedA, second.SharedA) && ReferenceEquals(first.SharedA.Leaf, first.SharedB.Leaf));
                                   }
                               }

                               sealed class Settings
                               {
                                   public Settings(string name) => Name = name;

                                   public string Name { get; }
                               }

                               sealed class SharedLeaf { }

                               sealed class SharedA
                               {
                                   public SharedA(SharedLeaf leaf) => Leaf = leaf;

                                   public SharedLeaf Leaf { get; }
                               }

                               sealed class SharedB
                               {
                                   public SharedB(SharedLeaf leaf) => Leaf = leaf;

                                   public SharedLeaf Leaf { get; }
                               }

                               sealed class Consumer
                               {
                                   public Consumer(Settings settings) => Settings = settings;

                                   public Settings Settings { get; }
                               }

                               sealed class Top
                               {
                                   public Top(SharedA sharedA, SharedB sharedB, Consumer a, Consumer b)
                                   {
                                       SharedA = sharedA;
                                       SharedB = sharedB;
                                       A = a;
                                       B = b;
                                   }

                                   public SharedA SharedA { get; }

                                   public SharedB SharedB { get; }

                                   public Consumer A { get; }

                                   public Consumer B { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<SharedLeaf>().As(Singleton).To<SharedLeaf>()
                                           .Bind<SharedA>().As(Singleton).To<SharedA>()
                                           .Bind<SharedB>().As(Singleton).To<SharedB>()
                                           .Bind<Consumer>().As(PerResolve).To<Consumer>()
                                           .Root<Func<Settings, Top>>("Create");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["one one two two", "True"], result);
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("new global::Sample.SharedLeaf()")).ShouldBe(1, result);
        result.GeneratedCode.ShouldContain("void EnsureSharedLeafExists");
    }

    [Theory]
    [InlineData("PerBlock", "one one two two three")]
    // PerResolve instances are shared by every lambda of the root, so the first call's instances are kept.
    [InlineData("PerResolve", "one one one one one")]
    public async Task ShouldKeepEachLambdasArgumentWhenTypesAreReachedFromSeveralLambdas(string lifetime, string values)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Holder holder = composition.Holder;
                                       Top first = holder.CreateTop(new Settings("one"));
                                       Top second = holder.CreateTop(new Settings("two"));
                                       Mid third = holder.CreateMid(new Settings("three"));
                                       Console.WriteLine($"{first.A.Leaf.Settings.Name} {first.B.Leaf.Settings.Name} {second.A.Leaf.Settings.Name} {second.B.Leaf.Settings.Name} {third.Leaf.Settings.Name}");
                                       Console.WriteLine(ReferenceEquals(first.Shared, second.Shared) && ReferenceEquals(first.Shared, third.Shared) && ReferenceEquals(first.Shared, holder.Shared));
                                   }
                               }

                               sealed class Settings
                               {
                                   public Settings(string name) => Name = name;

                                   public string Name { get; }
                               }

                               sealed class SharedLeaf { }

                               sealed class Shared
                               {
                                   public Shared(SharedLeaf a, SharedLeaf b) { }
                               }

                               sealed class Leaf
                               {
                                   public Leaf(Settings settings) => Settings = settings;

                                   public Settings Settings { get; }
                               }

                               sealed class Mid
                               {
                                   public Mid(Leaf leaf, Shared shared)
                                   {
                                       Leaf = leaf;
                                       Shared = shared;
                                   }

                                   public Leaf Leaf { get; }

                                   public Shared Shared { get; }
                               }

                               sealed class Top
                               {
                                   public Top(Mid a, Mid b, Shared shared)
                                   {
                                       A = a;
                                       B = b;
                                       Shared = shared;
                                   }

                                   public Mid A { get; }

                                   public Mid B { get; }

                                   public Shared Shared { get; }
                               }

                               sealed class Holder
                               {
                                   public Holder(Func<Settings, Top> createTop, Func<Settings, Mid> createMid, Shared shared)
                                   {
                                       CreateTop = createTop;
                                       CreateMid = createMid;
                                       Shared = shared;
                                   }

                                   public Func<Settings, Top> CreateTop { get; }

                                   public Func<Settings, Mid> CreateMid { get; }

                                   public Shared Shared { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<SharedLeaf>().As(Singleton).To<SharedLeaf>()
                                           .Bind<Shared>().As(Singleton).To<Shared>()
                                           .Bind<Leaf>().As(#lifetime#).To<Leaf>()
                                           .Bind<Mid>().As(#lifetime#).To<Mid>()
                                           .Root<Holder>("Holder");
                               }
                           }
                           """.Replace("#lifetime#", lifetime).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        // Leaf and Mid are reached inside two lambdas, each with its own argument, under the same binding ids.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([values, "True"], result);
    }

    [Fact(Skip = "Known limitation: a node that consumes the Func argument still gets no Ensure...Exists() helper, because a helper is shared by the whole root and cannot see a value overridden inside one lambda, so a diamond of argument consumers is still written out once per dependency path. Fixing it needs helpers that take the override values as parameters.")]
    public async Task ShouldCreateArgumentConsumingDiamondInOnePlace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }

                               // Each level has two PerResolve instances that both depend on both instances of the next level.
                               class A1 { public A1(A2 a, B2 b) { } }
                               class B1 { public B1(A2 a, B2 b) { } }
                               class A2 { public A2(A3 a, B3 b) { } }
                               class B2 { public B2(A3 a, B3 b) { } }
                               class A3 { public A3(A4 a, B4 b) { } }
                               class B3 { public B3(A4 a, B4 b) { } }
                               class A4 { public A4(Settings settings) { } }
                               class B4 { public B4(Settings settings) { } }

                               class Settings { }
                               class Top { public Top(A1 a, B1 b) { } }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<A1>().As(PerResolve).To<A1>()
                                           .Bind<B1>().As(PerResolve).To<B1>()
                                           .Bind<A2>().As(PerResolve).To<A2>()
                                           .Bind<B2>().As(PerResolve).To<B2>()
                                           .Bind<A3>().As(PerResolve).To<A3>()
                                           .Bind<B3>().As(PerResolve).To<B3>()
                                           .Bind<A4>().As(PerResolve).To<A4>()
                                           .Bind<B4>().As(PerResolve).To<B4>()
                                           .Root<Func<Settings, Top>>("Create");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("new global::Sample.A4(")).ShouldBe(1, result);
    }
}
