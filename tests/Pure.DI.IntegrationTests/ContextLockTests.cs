using System.Text.RegularExpressions;

namespace Pure.DI.IntegrationTests;

/// <summary>
/// A factory of a shared instance may synchronize its creation by ctx.Lock.
/// The instance is checked once more under the lock, so that it is created only once
/// even when the thread safety is disabled, see #174.
/// </summary>
public class ContextLockTests
{
    [Theory]
    [InlineData("Singleton", "Off")]
    [InlineData("Scoped", "Off")]
    [InlineData("Singleton", "On")]
    public async Task ShouldCheckSharedInstanceUnderContextLock(string lifetime, string threadSafe)
    {
        // Given

        // When
        var result = await CreateSource(
                threadSafe,
                $".Bind<Service>().As(Lifetime.{lifetime}).To(ctx => {{ lock (ctx.Lock) {{ return new Service(); }} }})")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        CountOfChecks(result, "Service").ShouldBe(2, result);
    }

    [Fact]
    public async Task ShouldCheckSharedInstanceUnderContextLockWhenContextHasOtherName()
    {
        // Given

        // When
        var result = await CreateSource(
                "Off",
                ".Bind<Service>().As(Lifetime.Singleton).To(context => { lock (context.Lock) { return new Service(); } })")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        CountOfChecks(result, "Service").ShouldBe(2, result);
    }

    [Fact]
    public async Task ShouldCheckSharedValueUnderContextLock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               struct Value
                               {
                                   public int Id;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup()
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Bind<Value>().As(Lifetime.Singleton).To(ctx => { lock (ctx.Lock) { return new Value { Id = 7 }; } })
                                           .Root<Value>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.Id);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["7"], result);
        Regex.Matches(result.GeneratedCode, @"if \(!_singletonValue\w*Created\)").Count.ShouldBe(2, result);
        result.GeneratedCode.ShouldContain("Threading.Thread.MemoryBarrier();", Case.Sensitive, result);
    }

    [Fact]
    public async Task ShouldNotCheckSharedInstanceTwiceWithoutContextLock()
    {
        // Given

        // When
        var result = await CreateSource(
                "Off",
                ".Bind<Service>().As(Lifetime.Singleton).To(ctx => new Service())")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        CountOfChecks(result, "Service").ShouldBe(1, result);
    }

    private static int CountOfChecks(Result result, string typeName) =>
        Regex.Matches(result.GeneratedCode, $@"if \(_(singleton|scoped){typeName}\w* is null\)").Count;

    private static string CreateSource(string threadSafe, string binding) =>
        """
        using System;
        using Pure.DI;

        namespace Sample
        {
            class Service
            {
            }

            partial class Composition
            {
                private static void Setup() =>
                    DI.Setup()
                        .Hint(Hint.ThreadSafe, "#threadSafe#")
                        #binding#
                        .Root<Service>("Root");
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
        """.Replace("#threadSafe#", threadSafe).Replace("#binding#", binding);
}
