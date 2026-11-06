namespace Pure.DI.IntegrationTests;

/// <summary>
/// A nested delegate creates its result instead of returning the argument of an outer delegate, see #185.
/// </summary>
public class Issue185Tests
{
    [Theory]
    [InlineData("ContextProvider provider, Func<IContext, Persister> persisterFactory", "provider.Open(\"c\") + \" \" + persisterFactory(new Context(\"a\"))", "c a b")]
    [InlineData("Func<IContext, Persister> persisterFactory, ContextProvider provider", "provider.Open(\"c\") + \" \" + persisterFactory(new Context(\"a\"))", "c a b")]
    [InlineData("Func<IContext, Persister> persisterFactory", "persisterFactory(new Context(\"a\")).ToString()", "a b")]
    public async Task ShouldCreateResultOfNestedDelegate(string appArgs, string appToString, string expected)
    {
        // Given

        // When
        var result = await $$"""
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public interface IContext;

                               public sealed class Context(string name) : IContext
                               {
                                   public override string ToString() => name;
                               }

                               public sealed class ContextProvider(Func<string, IContext> factory)
                               {
                                   public IContext Open(string name) => factory(name);
                               }

                               public sealed class Persister(IContext context, ContextProvider provider)
                               {
                                   public override string ToString() => $"{context} {provider.Open("b")}";
                               }

                               public sealed class App({{appArgs}})
                               {
                                   public override string ToString() => {{appToString}};
                               }

                               partial class Composition
                               {
                                   private static void Setup() => DI.Setup(nameof(Composition))
                                       .Bind<IContext>().To<Context>()
                                       .Bind().As(Singleton).To<ContextProvider>()
                                       .Root<App>("Root");
                               }

                               public static class Program
                               {
                                   public static void Main() => Console.WriteLine(new Composition().Root);
                               }
                           }
                           """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe([expected], result);
    }

    [Theory]
    [InlineData("Shared shared, Func<IContext, Item> items")]
    [InlineData("Func<IContext, Item> items, Shared shared")]
    public async Task ShouldCreateSharedInstanceOutsideDelegateWhenItIsReachedInsideDelegateToo(string appArgs)
    {
        // Given

        // When
        var result = await $$"""
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public interface IContext;

                               public sealed class Context : IContext { public override string ToString() => "root"; }

                               public sealed class Arg(string name) : IContext { public override string ToString() => name; }

                               public sealed class Shared(IContext context) { public override string ToString() => $"{context}"; }

                               public sealed class Item(Shared shared) { public override string ToString() => $"{shared}"; }

                               public sealed class App({{appArgs}})
                               {
                                   public override string ToString() => shared + " " + items(new Arg("a"));
                               }

                               partial class Composition
                               {
                                   private static void Setup() => DI.Setup(nameof(Composition))
                                       .Bind<IContext>().To<Context>()
                                       .Bind().As(Singleton).To<Shared>()
                                       .Root<App>("Root");
                               }

                               public static class Program
                               {
                                   public static void Main() => Console.WriteLine(new Composition().Root);
                               }
                           }
                           """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        // The shared instance is valid everywhere only when it is created outside the delegate,
        // so it does not depend on the order in which the composition reaches it.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["root root"], result);
    }
}
