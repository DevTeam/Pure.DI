namespace Pure.DI.IntegrationTests;

using System.Text;
using Core;

/// <summary>
/// A singleton that reads an argument of a delegate is created once, when screens on a cycle through delegates reach it, see #193.
/// </summary>
public class Issue193Tests
{
    [Fact]
    public async Task ShouldCreateSingletonReadingDelegateArgumentOnCycle()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public sealed class Settings
                               {
                                   private readonly string _path;
                                   public Settings(string path) => _path = path;
                                   public override string ToString() => _path;
                               }

                               public sealed class Screen0
                               {
                                   private readonly Settings _settings;
                                   public Screen0(Settings settings, Func<string, Screen1> openScreen1, Func<string, Screen0> openScreen0) => _settings = settings;
                                   public override string ToString() => _settings.ToString();
                               }

                               public sealed class Screen1
                               {
                                   private readonly Settings _settings;
                                   public Screen1(Settings settings, Func<string, Screen0> openScreen0, Func<string, Screen1> openScreen1) => _settings = settings;
                                   public override string ToString() => _settings.ToString();
                               }

                               public sealed class App
                               {
                                   private readonly Func<string, Screen0> _open;
                                   public App(Func<string, Screen0> open) => _open = open;
                                   public override string ToString() => _open("title").ToString();
                               }

                               partial class Composition
                               {
                                   private static void Setup() => DI.Setup(nameof(Composition))
                                       .Bind<string>().To(_ => "path")
                                       .Bind().As(Singleton).To<Settings>()
                                       .Root<App>("Root");
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
        // The singleton keeps the argument of the first call of the delegate, which is reported.
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Select(i => i.Id).ShouldBe([LogId.WarningStoredLifetimeFromDelegateArgument], result);
        result.StdOut.ShouldBe(["title"], result);
        var lines = result.GeneratedCode.Split(Environment.NewLine);
        lines.Count(i => i.Contains("new global::Sample.Settings(")).ShouldBe(2, result.GeneratedCode);
    }

    // Each screen opens the next two, so the paths through the delegates on the cycle double per screen.
    // The singletons are created by the local function of each screen, so their code grows linearly with the number of screens.
    [Theory]
    [InlineData(4)]
    [InlineData(12)]
    [InlineData(24)]
    public async Task ShouldCreateSingletonsReadingDelegateArgumentOnceInEachScreen(int size)
    {
        // Given
        var classes = new StringBuilder();
        for (var i = 0; i < size; i++)
        {
            var a = (i + 1) % size;
            var b = (i + 2) % size;
            classes.AppendLine(
                $$"""
                      public sealed class Screen{{i}}
                      {
                          private readonly Settings _settings;
                          private readonly Storage _storage;
                          private readonly Func<string, Screen{{a}}> _openA;
                          public Screen{{i}}(Settings settings, Storage storage, Func<string, Screen{{a}}> openA, Func<string, Screen{{b}}> openB)
                          {
                              _settings = settings;
                              _storage = storage;
                              _openA = openA;
                          }

                          public string Walk(int steps) => $"S{{i}}({_settings},{_storage})" + (steps > 0 ? " " + _openA("next").Walk(steps - 1) : "");
                      }
                  """);
        }

        // When
        var result = await $$"""
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public sealed class Settings
                               {
                                   private readonly string _path;
                                   public Settings(string path) => _path = path;
                                   public override string ToString() => _path;
                               }

                               public sealed class Storage
                               {
                                   private readonly Settings _settings;
                                   private readonly string _name;
                                   public Storage(Settings settings, string name) { _settings = settings; _name = name; }
                                   public override string ToString() => $"{_name}:{_settings}";
                               }

                           {{classes}}

                               public sealed class App
                               {
                                   private readonly Func<string, Screen0> _open;
                                   public App(Func<string, Screen0> open) => _open = open;
                                   public override string ToString() => _open("title").Walk(2);
                               }

                               partial class Composition
                               {
                                   private static void Setup() => DI.Setup(nameof(Composition))
                                       .Bind<string>().To(_ => "path")
                                       .Bind().As(Singleton).To<Settings>()
                                       .Bind().As(Singleton).To<Storage>()
                                       .Root<App>("Root");
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
                           """.RunAsync(new Options(Options.ModernLanguageVersion), TimeSpan.FromSeconds(60));

        // Then
        // The singletons keep the argument of the first call of the delegate.
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Select(i => i.Id).Distinct().ShouldBe([LogId.WarningStoredLifetimeFromDelegateArgument], result);
        result.StdOut.ShouldBe(["S0(title,title:title) S1(title,title:title) S2(title,title:title)"], result);
        var lines = result.GeneratedCode.Split(Environment.NewLine);
        lines.Count(i => i.Contains("new global::Sample.Settings(")).ShouldBeLessThanOrEqualTo(size, result.GeneratedCode);
        lines.Count(i => i.Contains("new global::Sample.Storage(")).ShouldBeLessThanOrEqualTo(size, result.GeneratedCode);
    }
}
