using System.Text;

namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
/// Screens that open each other through delegates with arguments are each created in one place,
/// so the code grows linearly with the number of screens, not with the number of navigation paths, see #192.
/// </summary>
public class Issue192Tests
{
    private static readonly int[] Sizes = [4, 8, 12];

    [Theory]
    [InlineData(false, "Auto")]
    [InlineData(false, "On")]
    [InlineData(true, "Auto")]
    public async Task ShouldCreateEachScreenOnCycleThroughDelegatesOnce(bool letFunc, string localFunctions)
    {
        // Given

        foreach (var size in Sizes)
        {
            // When
            var result = await Create(size, letFunc, localFunctions).RunAsync(new Options(Options.ModernLanguageVersion));

            // Then
            result.Success.ShouldBeTrue(result);
            result.StdOut.ShouldBe([GetExpectedPath(size)], result);
            var lines = result.GeneratedCode.Split(Environment.NewLine);
            for (var screen = 0; screen < size; screen++)
            {
                lines.Count(i => i.Contains($"new global::Sample.Screen{screen}(")).ShouldBe(1, $"Screen{screen} of {size} screens{Environment.NewLine}{result.GeneratedCode}");
            }
        }
    }

    // Each screen opens the next two, so the path visits the screens by steps of one and two.
    private static string GetExpectedPath(int size)
    {
        var path = new StringBuilder();
        var screen = 0;
        var context = "start";
        for (var step = 0; step < Steps; step++)
        {
            path.Append($"S{screen}({context},N{step}) ");
            var next = (screen + 1 + step % 2) % size;
            context = $"c{step}";
            screen = next;
        }

        path.Append($"S{screen}({context},N{Steps})");
        return path.ToString();
    }

    private const int Steps = 7;

    private static string Create(int size, bool letFunc, string localFunctions)
    {
        var classes = new StringBuilder();
        for (var i = 0; i < size; i++)
        {
            var a = (i + 1) % size;
            var b = (i + 2) % size;
            classes.AppendLine(
                $$"""
                      public sealed class Context{{i}}
                      {
                          public Context{{i}}(string value) => Value = value;

                          public string Value { get; }
                      }

                      public sealed class Screen{{i}} : IScreen
                      {
                          private readonly Context{{i}} _context;
                          private readonly INavigation _navigation;
                          private readonly Func<INavigation, Context{{a}}, Screen{{a}}> _openA;
                          private readonly Func<INavigation, Context{{b}}, Screen{{b}}> _openB;

                          public Screen{{i}}(
                              Context{{i}} context,
                              INavigation navigation,
                              Func<INavigation, Context{{a}}, Screen{{a}}> openA,
                              Func<INavigation, Context{{b}}, Screen{{b}}> openB)
                          {
                              _context = context;
                              _navigation = navigation;
                              _openA = openA;
                              _openB = openB;
                          }

                          public string Run(int step, int steps)
                          {
                              var current = $"S{{i}}({_context.Value},{_navigation})";
                              if (step == steps)
                              {
                                  return current;
                              }

                              var navigation = new Navigation(step + 1);
                              IScreen next = step % 2 == 0
                                  ? _openA(navigation, new Context{{a}}($"c{step}"))
                                  : _openB(navigation, new Context{{b}}($"c{step}"));
                              return current + " " + next.Run(step + 1, steps);
                          }
                      }

                  """);
        }

        var funcBinding = letFunc
            ? """
                          .Bind<Func<TT1, TT2, TT>>()
                              .As(Lifetime.PerBlock)
                              .To(ctx => new Func<TT1, TT2, TT>((TT1 arg1, TT2 arg2) =>
                              {
                                  ctx.Let<TT1>(arg1);
                                  ctx.Let<TT2>(arg2);
                                  ctx.Inject<TT>(ctx.Tag, out var value);
                                  return value;
                              }))
              """
            : "";

        return $$"""
                 using System;
                 using Pure.DI;

                 namespace Sample
                 {
                     public interface INavigation;

                     public sealed class Navigation : INavigation
                     {
                         private readonly int _id;

                         public Navigation(int id) => _id = id;

                         public override string ToString() => $"N{_id}";
                     }

                     public interface IScreen
                     {
                         string Run(int step, int steps);
                     }

                 {{classes}}
                     partial class Composition
                     {
                         private static void Setup() =>
                             DI.Setup(nameof(Composition))
                                 .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                 {{funcBinding}}
                                 .Root<Func<INavigation, Context0, Screen0>>("Root");
                     }

                     public static class Program
                     {
                         public static void Main() =>
                             Console.WriteLine(new Composition().Root(new Navigation(0), new Context0("start")).Run(0, {{Steps}}));
                     }
                 }
                 """;
    }
}
