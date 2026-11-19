namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
/// A node on a cycle through a lambda that overrides the argument of a delegate can also be reached
/// by a path that does not pass through the lambda, for example, from a root, see #204.
/// </summary>
public class UnresolvedArgumentOnCycleTests
{
    // The root of FileSystem reaches it without the argument of the delegate, and nothing else provides the argument.
    [Theory]
    [InlineData("record FileSystem(bool Flag, Func<Service> Service);")]
    [InlineData("record FileSystem(bool Flag, Notify Notify); record Notify(Lazy<Service> Service);")]
    [InlineData("record FileSystem(Helper Helper, Func<Service> Service); record Helper(bool Flag);")]
    public async Task ShouldReportUnresolvedArgumentWhenRootBypassesLambdaOnCycle(string fileSystem)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;

                             namespace Sample;

                             partial class Composition
                             {
                                 private static void Setup() =>
                                     DI.Setup(nameof(Composition))
                                         .Root<Service>("Service")
                                         .Root<FileSystem>("FileSystem");
                             }

                             record Service(Func<bool, FileSystem> Create);
                             {{fileSystem}}

                             class Program { static void Main() { } }
                             """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeFalse(result);
        result.Errors.Select(i => i.Id).ShouldBe([LogId.ErrorUnableToResolve], result.ToString());
    }

    // The composition from #204: the roots of a lightweight root reach FileSystem without the argument of the delegate.
    [Theory]
    [InlineData("Singleton")]
    [InlineData("Transient")]
    public async Task ShouldReportUnresolvedArgumentForLightweightRootOnCycle(string lifetime)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;

                             namespace Sample;

                             partial class Composition
                             {
                                 private static void Setup() =>
                                     DI.Setup(nameof(Composition))
                                         .Hint(Hint.ThreadSafe, "Off")
                                         .DefaultLifetime(Lifetime.{{lifetime}})
                                         .Bind<Dialogs>().To<Dialogs>()
                                         .Bind<Notify>().To<Notify>()
                                         .Root<Service>("Service")
                                         .Root<Notify>()
                                         .Root<Dialogs>()
                                         .Root<FileSystem>();
                             }

                             record Service(Lazy<Dialogs> Dialogs, Func<bool, FileSystem> Create);
                             record FileSystem(bool Flag, Notify Notify);
                             record Notify(Lazy<Dialogs> Dialogs);
                             record Dialogs(Func<string, Dialog> Create);
                             record Dialog(string Name, Func<bool, FileSystem> Create);

                             class Program { static void Main() { } }
                             """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeFalse(result);
        result.Errors.Select(i => i.Id).ShouldBe([LogId.ErrorUnableToResolve], result.ToString());
    }

    // Every path to FileSystem passes through the lambda, so it reads the argument of the delegate.
    [Theory]
    [InlineData("record FileSystem(bool Flag, Func<Service> Service);", "fileSystem.Flag", "fileSystem.Service().Create(false).Flag")]
    [InlineData("record FileSystem(Helper Helper, Func<Service> Service); record Helper(bool Flag);", "fileSystem.Helper.Flag", "fileSystem.Service().Create(false).Helper.Flag")]
    public async Task ShouldPassArgumentWhenEveryPathPassesThroughLambdaOnCycle(string fileSystem, string flag, string nextFlag)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;

                             namespace Sample;

                             partial class Composition
                             {
                                 private static void Setup() =>
                                     DI.Setup(nameof(Composition))
                                         .Root<Service>("Service");
                             }

                             record Service(Func<bool, FileSystem> Create);
                             {{fileSystem}}

                             class Program
                             {
                                 static void Main()
                                 {
                                     var fileSystem = new Composition().Service.Create(true);
                                     Console.WriteLine({{flag}});
                                     Console.WriteLine({{nextFlag}});
                                 }
                             }
                             """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False"], result);
    }

    // The value can be created, so the root creates it and the lambda passes its argument.
    [Fact]
    public async Task ShouldCreateBoundValueWhenRootBypassesLambdaOnCycle()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           partial class Composition
                           {
                               private static void Setup() =>
                                   DI.Setup(nameof(Composition))
                                       .Bind<bool>().To(_ => true)
                                       .Root<Service>("Service")
                                       .Root<FileSystem>("FileSystem");
                           }

                           record Service(Func<bool, FileSystem> Create);
                           record FileSystem(bool Flag, Func<Service> Service);

                           class Program
                           {
                               static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.FileSystem.Flag);
                                   Console.WriteLine(composition.FileSystem.Service().Create(false).Flag);
                                   Console.WriteLine(composition.Service.Create(false).Flag);
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False", "False"], result);
    }
}
