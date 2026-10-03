namespace Pure.DI.IntegrationTests;

/// <summary>
///     Reproduces the "simplified bindings expose disposal interfaces" issue observed in
///     AI.Client: a parameterless <c>Bind()</c> exposes the <c>System.IAsyncDisposable</c>
///     contract of the implementation, although <c>System.IDisposable</c> is a special type and
///     is not exposed. As soon as two implementations share that interface, the generator reports
///     <c>DIW000: The binding for System.IAsyncDisposable has been overridden.</c>
/// </summary>
public class SimplifiedBindingDisposalContractTests
{
    [Fact]
    public async Task ShouldNotExposeAsyncDisposableContractForSimplifiedBindings()
    {
        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample;

                           class FirstAsyncDisposable : IAsyncDisposable
                           {
                               public ValueTask DisposeAsync() => ValueTask.CompletedTask;
                           }

                           class SecondAsyncDisposable : IAsyncDisposable
                           {
                               public ValueTask DisposeAsync() => ValueTask.CompletedTask;
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind().To<FirstAsyncDisposable>()
                                   .Bind().To<SecondAsyncDisposable>()
                                   .Root<FirstAsyncDisposable>("First")
                                   .Root<SecondAsyncDisposable>("Second");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().First.ToString() + new Composition().Second.ToString());
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Errors.ShouldBeEmpty();
        result.Warnings.ShouldBeEmpty(result);
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldNotExposeDisposableContractForSimplifiedBindings()
    {
        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           class FirstDisposable : IDisposable
                           {
                               public void Dispose() {}
                           }

                           class SecondDisposable : IDisposable
                           {
                               public void Dispose() {}
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind().To<FirstDisposable>()
                                   .Bind().To<SecondDisposable>()
                                   .Root<FirstDisposable>("First")
                                   .Root<SecondDisposable>("Second");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().First.ToString() + new Composition().Second.ToString());
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Errors.ShouldBeEmpty();
        result.Warnings.ShouldBeEmpty(result);
        result.Success.ShouldBeTrue(result);
    }
}
