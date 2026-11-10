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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShouldCreateGraphUnderNestedFuncsOnceWhenEachFuncPassesConsumedArgument(bool passesOneArgument)
    {
        // Given
        const int levels = 12;

        // When
        // A view model that reads the argument of a delegate was created in place in each lambda,
        // so each lambda repeated the whole graph of the next view models, see #178.
        // It is now created by a local function that gets the overridden values as arguments.
        var result = await CreateShadowedArgumentsSetup(levels, passesOneArgument)
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([passesOneArgument ? $"Vm{levels} {levels - 1} {levels}" : $"Vm{levels} {levels}"], result);
        var lines = result.GeneratedCode.Split(Environment.NewLine);
        lines.Count(i => i.Contains($"new global::Sample.Vm{levels}(")).ShouldBeLessThan(levels, result);
    }

    private static string CreateShadowedArgumentsSetup(int levels, bool passesOneArgument)
    {
        // With passesOneArgument, each view model consumes Nav and ArgB, and each delegate passes only one of them.
        // Otherwise, each view model consumes Nav, and both delegates pass it.
        var args = passesOneArgument ? "Nav nav, ArgB b" : "Nav nav";
        var openWithB = passesOneArgument ? "Func<ArgB, Vm{0}>" : "Func<Nav, ArgB, Vm{0}>";
        var callWithB = passesOneArgument ? "OpenWithB(new ArgB(value))" : "OpenWithB(new Nav(value), new ArgB(value))";
        var classes = new StringBuilder();
        for (var i = 1; i < 10; i++)
        {
            classes.AppendLine($"public sealed class S{i} {{ public S{i}(S{i - 1} previous) {{ }} }}");
        }

        for (var i = 0; i < levels; i++)
        {
            var openWithBType = string.Format(openWithB, i + 1);
            classes.AppendLine(
                $$"""
                  public sealed class Vm{{i}} : IVm
                  {
                      public Vm{{i}}({{args}}, S9 services, Func<Nav, Vm{{i + 1}}> open, {{openWithBType}} openWithB)
                      {
                          Open = open;
                          OpenWithB = openWithB;
                      }

                      public Func<Nav, Vm{{i + 1}}> Open { get; }

                      public {{openWithBType}} OpenWithB { get; }

                      public IVm Next(int index)
                      {
                          var value = index.ToString();
                          return index % 2 == 0 ? {{callWithB}} : Open(new Nav(value));
                      }
                  }
                  """);
        }

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

                     public sealed record Nav(string Value);

                     public sealed record ArgB(string Value);

                     public sealed class S0 { }

                     {{classes}}

                     public sealed class Vm{{levels}} : IVm
                     {
                         private readonly string _text;

                         public Vm{{levels}}({{args}}, S9 services) => _text = GetType().Name + " " + nav.Value{{(passesOneArgument ? " + \" \" + b.Value" : "")}};

                         public IVm Next(int index) => this;

                         public override string ToString() => _text;
                     }

                     public sealed class Top
                     {
                         public Top(Func<Nav, ArgB, Vm0> open) => Open = open;

                         public Func<Nav, ArgB, Vm0> Open { get; }
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
                             IVm vm = new Composition().Root.Open(new Nav("0"), new ArgB("0"));
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

    // The code of a view model created by the lambdas of nested delegates grows linearly with the number of view models, see #184.
    private static readonly int[] NestedViewModelSizes = [4, 6, 10];
    // A view model reached with a single argument or without it has a copy for each of these cases,
    // so the code of the first view models differs from the code of the others.
    private static readonly int[] NestedViewModelBuiltInFuncSizes = [8, 12, 20];
    private const double NestedViewModelMaxGrowthRatio = 3;

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
        var sizes = letFunc ? NestedViewModelSizes : NestedViewModelBuiltInFuncSizes;
        var lines = new List<int>();
        var constructions = new List<int>();
        foreach (var size in sizes)
        {
            var result = await CreateNestedViewModels(size, letFunc).RunAsync(new Options(Options.ModernLanguageVersion));

            // Then
            result.Success.ShouldBeTrue(result);
            result.StdOut.ShouldBe([$"Vm{size}"], result);
            lines.Add(result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("global::Sample.")));
            constructions.Add(result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains($"new global::Sample.Vm{size}(")));
        }

        var firstStep = Math.Max(lines[1] - lines[0], 1);
        var lastStep = lines[2] - lines[1];
        ((double)lastStep / firstStep).ShouldBeLessThan(
            NestedViewModelMaxGrowthRatio,
            $"Generated lines for sizes {string.Join(", ", sizes)}: {string.Join(", ", lines)}; constructions of the last view model: {string.Join(", ", constructions)}");
    }

    [Fact]
    public async Task ShouldNotWarnWhenNestedFuncsOverrideArguments()
    {
        // Given

        // When
        var result = await CreateNestedViewModels(12, letFunc: false).RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Vm12"], result);
    }

    private static string CreateNestedViewModels(int size, bool letFunc = true)
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

    // A nested delegate creates its result instead of returning the argument of an outer delegate, see #185.
#if ROSLYN4_8_OR_GREATER
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
#endif
}
