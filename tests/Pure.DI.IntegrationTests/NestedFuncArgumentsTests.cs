using System.Text;

namespace Pure.DI.IntegrationTests;

/// <summary>
/// View models that open each other through nested <c>Func&lt;T1, T&gt;</c> delegates with arguments, see #175 and #176.
/// </summary>
public class NestedFuncArgumentsTests
{
    [Fact]
    public async Task ShouldCreateGraphUnderNestedFuncsOnceForUnconsumedArguments()
    {
        // Given
        const int levels = 16;

        // When
        // The graph under each delegate was created once for each chain of delegates above it,
        // so the code doubled with each level and this depth would not end in a reasonable time.
        var result = await CreateSetup(levels, "")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([$"Vm{levels}"], result);
        var lines = result.GeneratedCode.Split(Environment.NewLine);
        lines.Count(i => i.Contains("new global::Sample.Vm1(")).ShouldBe(1, result);
        lines.Count(i => i.Contains($"new global::Sample.Vm{levels}(")).ShouldBeLessThan(levels, result);
    }

    [Fact]
    public async Task ShouldPassArgumentOfNestedFuncsToConsumer()
    {
        // Given
        const int levels = 6;

        // When
        var result = await CreateSetup(levels, "ArgA arg")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([$"Vm{levels} 5"], result);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    public async Task ShouldPassArgumentOfNestedFuncsToEachConsumer(int levels)
    {
        // Given

        // When
        // Each view model consumes the argument, so the graph under each delegate is rewritten in a scope of its own
        // and the scopes are numerous, which made the rewrite several times slower than before #172, see #176.
        var result = await CreateSetup(levels, "ArgA arg", "ArgA arg")
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        // The view models are opened by Func<ArgA, T> at odd indexes.
        result.StdOut.ShouldBe([$"Vm{levels} {(levels % 2 == 1 ? levels : levels - 1)}"], result);
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("On")]
    public async Task ShouldKeepPerBlockInstancesOfInstanceUnderFuncs(string mode)
    {
        // Given

        // When
        var result = await $$"""
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public sealed class ArgA { }
                               public sealed class ArgB { }
                               public sealed class Dep { }
                               public sealed class Own { }

                               public sealed class Child
                               {
                                   public Child(Dep dep, Own own, Own otherOwn)
                                   {
                                       Dep = dep;
                                       Own = own;
                                       OtherOwn = otherOwn;
                                   }

                                   public Dep Dep { get; }

                                   public Own Own { get; }

                                   public Own OtherOwn { get; }
                               }

                               public sealed class Vm
                               {
                                   public Vm(Dep dep, Func<ArgA, Child> openWithA, Func<ArgB, Child> openWithB)
                                   {
                                       Dep = dep;
                                       OpenWithA = openWithA;
                                       OpenWithB = openWithB;
                                   }

                                   public Dep Dep { get; }

                                   public Func<ArgA, Child> OpenWithA { get; }

                                   public Func<ArgB, Child> OpenWithB { get; }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.LocalFunctions, "{{mode}}")
                                           .Bind<Dep>().As(Lifetime.PerBlock).To<Dep>()
                                           .Bind<Own>().As(Lifetime.PerBlock).To<Own>()
                                           .Root<Vm>("Root");
                               }

                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var vm = new Composition().Root;
                                       var first = vm.OpenWithA(new ArgA());
                                       var second = vm.OpenWithB(new ArgB());
                                       Console.WriteLine(ReferenceEquals(vm.Dep, first.Dep) && ReferenceEquals(vm.Dep, second.Dep));
                                       Console.WriteLine(ReferenceEquals(first.Own, first.OtherOwn) && ReferenceEquals(second.Own, second.OtherOwn));
                                       Console.WriteLine(ReferenceEquals(first.Own, second.Own));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        // An instance created inside a lambda shares the PerBlock instances of the code around the lambda
        // and creates its own PerBlock instances once per call.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "False"], result);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public async Task ShouldCreateArgumentWhenNoNestedFuncProvidesIt(int levels)
    {
        // Given

        // When
        // On the paths that pass only through Func<ArgA, T>, no Func<ArgB, T> encloses the last view model,
        // so it gets an ArgB created by the composition instead of the argument of a delegate on another path.
        var result = await CreateSetup(levels, "ArgB arg", constructibleArgB: true)
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        // The view models are opened by Func<ArgB, T> at even indexes.
        result.StdOut.ShouldBe([$"Vm{levels} {levels}"], result);
    }

    private static string CreateSetup(int levels, string lastArgs, string args = "", bool constructibleArgB = false)
    {
        var classes = new StringBuilder();
        for (var i = 1; i < 10; i++)
        {
            classes.AppendLine($"public sealed class S{i} {{ public S{i}(S{i - 1} previous) {{ }} }}");
        }

        for (var i = 0; i < levels; i++)
        {
            classes.AppendLine(
                $$"""
                  public sealed class Vm{{i}} : IVm
                  {
                      public Vm{{i}}(S9 services, {{(args == "" ? "" : args + ", ")}}Func<ArgA, Vm{{i + 1}}> openWithA, Func<ArgB, Vm{{i + 1}}> openWithB)
                      {
                          OpenWithA = openWithA;
                          OpenWithB = openWithB;
                      }

                      public Func<ArgA, Vm{{i + 1}}> OpenWithA { get; }

                      public Func<ArgB, Vm{{i + 1}}> OpenWithB { get; }

                      public IVm Next(int index) => index % 2 == 0 ? OpenWithB(new ArgB(index.ToString())) : OpenWithA(new ArgA(index.ToString()));

                      public override string ToString() => GetType().Name;
                  }
                  """);
        }

        var lastParameters = lastArgs == "" ? "S9 services" : $"S9 services, {lastArgs}";
        var lastToString = lastArgs == "" ? "GetType().Name" : "GetType().Name + \" \" + arg.Value";
        var bindings = new StringBuilder();
        for (var i = 0; i < 10; i++)
        {
            bindings.AppendLine($".Bind<S{i}>().As(Lifetime.Singleton).To<S{i}>()");
        }

        return $$"""
                 using System;
                 using Pure.DI;

                 namespace Sample
                 {
                     public interface IVm
                     {
                         IVm Next(int index);
                     }

                     public sealed class ArgA
                     {
                         public ArgA(string value) => Value = value;

                         public string Value { get; }
                     }

                     public sealed class ArgB
                     {
                         {{(constructibleArgB ? "public ArgB() => Value = \"new\";" : "")}}

                         public ArgB(string value) => Value = value;

                         public string Value { get; }
                     }

                     public sealed class S0 { }

                     {{classes}}

                     public sealed class Vm{{levels}} : IVm
                     {
                         private readonly string _text;

                         public Vm{{levels}}({{lastParameters}}) => _text = {{lastToString}};

                         public IVm Next(int index) => this;

                         public override string ToString() => _text;
                     }

                     public sealed class Top
                     {
                         public Top(Func<ArgA, Vm0> open) => Open = open;

                         public Func<ArgA, Vm0> Open { get; }
                     }

                     partial class Composition
                     {
                         private static void Setup() =>
                             DI.Setup(nameof(Composition))
                                 {{bindings}}
                                 .Root<Top>("Root");
                     }

                     public static class Program
                     {
                         public static void Main()
                         {
                             IVm vm = new Composition().Root.Open(new ArgA("0"));
                             for (var index = 1; index <= {{levels}}; index++)
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
