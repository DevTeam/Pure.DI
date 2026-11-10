using System.Text;

namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
/// The code of a view model created by the lambdas of nested delegates grows linearly with the number of view models, see #184.
/// </summary>
public class Issue184Tests
{
    private static readonly int[] Sizes = [4, 6, 10];
    // A view model reached with a single argument or without it has a copy for each of these cases,
    // so the code of the first view models differs from the code of the others.
    private static readonly int[] BuiltInFuncSizes = [8, 12, 20];
    private const double MaxGrowthRatio = 3;

    // The built-in delegates override their argument, which reaches the view models of nested delegates too,
    // so a view model is reached with each combination of the arguments passed along the way. It is created once
    // for all of them, and its code checks whether an argument is passed.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldCreateViewModelOfNestedFuncsOncePerVariant(bool letFunc)
    {
        // Given

        // When
        var sizes = letFunc ? Sizes : BuiltInFuncSizes;
        var lines = new List<int>();
        var constructions = new List<int>();
        foreach (var size in sizes)
        {
            var result = await Create(size, letFunc).RunAsync(new Options(Options.ModernLanguageVersion));

            // Then
            result.Success.ShouldBeTrue(result);
            result.StdOut.ShouldBe([$"Vm{size}"], result);
            lines.Add(result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("global::Sample.")));
            constructions.Add(result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains($"new global::Sample.Vm{size}(")));
        }

        var firstStep = Math.Max(lines[1] - lines[0], 1);
        var lastStep = lines[2] - lines[1];
        ((double)lastStep / firstStep).ShouldBeLessThan(
            MaxGrowthRatio,
            $"Generated lines for sizes {string.Join(", ", sizes)}: {string.Join(", ", lines)}; constructions of the last view model: {string.Join(", ", constructions)}");
    }

    [Fact]
    public async Task ShouldNotWarnWhenNestedFuncsOverrideArguments()
    {
        // Given

        // When
        var result = await Create(12, letFunc: false).RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Vm12"], result);
    }

    private static string Create(int size, bool letFunc = true)
    {
        var services = string.Join(", ", Enumerable.Range(0, size).Select(i => $"A{i} a{i}"));
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        for (var i = 0; i < size; i++)
        {
            classes.AppendLine($"public sealed class A{i} {{ }}");
            bindings.AppendLine($".Bind<A{i}>().To<A{i}>()");
            classes.AppendLine(
                $$"""
                  public sealed class Vm{{i}} : IVm
                  {
                      private readonly Func<A{{i}}, Vm{{i + 1}}> _openWith;
                      private readonly Func<Vm{{i + 1}}> _open;

                      public Vm{{i}}({{services}}, Func<A{{i}}, Vm{{i + 1}}> openWith, Func<Vm{{i + 1}}> open)
                      {
                          _openWith = openWith;
                          _open = open;
                      }

                      public IVm Next(int index) => index % 2 == 0 ? _openWith(new A{{i}}()) : _open();
                  }
                  """);
        }

        // The argument of a delegate reaches only the view model it creates.
        var funcBinding = letFunc
            ? """
              .Bind<Func<TT1, TT>>().As(Lifetime.PerBlock).To(ctx => new Func<TT1, TT>(arg1 =>
              {
                  ctx.Let<TT1>(arg1);
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
                     public interface IVm
                     {
                         IVm Next(int index);
                     }

                     {{classes}}

                     public sealed class Vm{{size}} : IVm
                     {
                         public Vm{{size}}({{services}}) { }

                         public IVm Next(int index) => this;

                         public override string ToString() => GetType().Name;
                     }

                     partial class Composition
                     {
                         private static void Setup() =>
                             DI.Setup(nameof(Composition))
                                 {{funcBinding}}
                                 {{bindings}}
                                 .Root<Vm0>("Root");
                     }

                     public static class Program
                     {
                         public static void Main()
                         {
                             IVm vm = new Composition().Root;
                             for (var index = 1; index <= {{size}}; index++)
                             {
                                 vm = vm.Next(index);
                             }

                             Console.WriteLine(vm);
                         }
                     }
                 }
                 """;
    }
}
