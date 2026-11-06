using System.Text;

namespace Pure.DI.IntegrationTests;

using Core;

#if ROSLYN4_8_OR_GREATER
/// <summary>
/// A shared instance reached inside the lambdas of delegates with arguments is created in one place
/// when it does not read those arguments itself, see #186.
/// </summary>
public class Issue186Tests
{
    private static readonly int[] Sizes = [4, 6, 10];

    [Theory]
    [InlineData("Singleton", false, "On")]
    [InlineData("Singleton", false, "Auto")]
    [InlineData("Singleton", true, "On")]
    [InlineData("Scoped", false, "On")]
    public async Task ShouldCreateSharedInstanceUnderDelegateArgumentsOnce(string lifetime, bool letFunc, string localFunctions)
    {
        // Given

        foreach (var size in Sizes)
        {
            // When
            var result = await Create(size, lifetime, letFunc, localFunctions).RunAsync(new Options(Options.ModernLanguageVersion));

            // Then
            result.Success.ShouldBeTrue(result);
            result.StdOut.ShouldBe([string.Concat(Enumerable.Range(0, size).Select(i => $"S{i}"))], result);
            var lines = result.GeneratedCode.Split(Environment.NewLine);
            for (var level = 1; level < size; level++)
            {
                lines.Count(i => i.Contains($"new global::Sample.Service{level}(")).ShouldBe(1, $"Service{level} of {size} levels{Environment.NewLine}{result.GeneratedCode}");
            }
        }
    }

    [Fact]
    public async Task ShouldCreateSharedInstanceThatReadsDelegateArgumentInsideLambdas()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public sealed class A(string name)
                               {
                                   public string Name => name;
                               }

                               public sealed class Shared(A a)
                               {
                                   public A A => a;
                               }

                               public sealed class Item1(A a, Shared shared)
                               {
                                   public string Value => $"{a.Name}:{shared.A.Name}";
                               }

                               public sealed class Item2(A a, Shared shared)
                               {
                                   public string Value => $"{a.Name}:{shared.A.Name}";
                               }

                               public sealed class Service(Func<A, Item1> open1, Func<A, Item2> open2)
                               {
                                   public string Run() => $"{open1(new A("1")).Value} {open2(new A("2")).Value}";
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.LocalFunctions, "On")
                                           .Bind().As(Lifetime.Singleton).To<Shared>()
                                           .Root<Service>("Root");
                               }

                               public static class Program
                               {
                                   public static void Main() => Console.WriteLine(new Composition().Root.Run());
                               }
                           }
                           """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        // The shared instance is created in the lambda that is called first, so it keeps the argument of that call.
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Select(i => i.Id).ShouldBe([LogId.WarningStoredLifetimeFromDelegateArgument], result);
        result.StdOut.ShouldBe(["1:1 2:1"], result);
    }

    [Fact]
    public async Task ShouldCreateSharedInstancesOnCycleThroughDelegateArgumentsOnce()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public sealed class User(string name)
                               {
                                   public string Name => name;
                               }

                               public sealed class Condition(string name)
                               {
                                   public string Name => name;
                               }

                               public sealed class Context(User user, ConfigurationProvider configuration)
                               {
                                   public string Value => $"{user.Name}{configuration.Name}";
                               }

                               public sealed class ContextProvider(Func<User, Context> open)
                               {
                                   public Context Open(string name) => open(new User(name));
                               }

                               public sealed class ConfigurationProvider(Lazy<ProjectionService> projection)
                               {
                                   public string Name => projection.Value.Name;
                               }

                               public sealed class ProjectionService(ContextProvider contexts)
                               {
                                   public string Name => contexts is null ? "!" : "P";
                               }

                               public sealed class Evaluator1(Condition condition, ContextProvider contexts)
                               {
                                   public string Value => condition.Name + contexts.Open("u").Value;
                               }

                               public sealed class Evaluator2(Condition condition, ContextProvider contexts)
                               {
                                   public string Value => condition.Name + contexts.Open("v").Value;
                               }

                               public sealed class Evaluator3(Condition condition, ContextProvider contexts)
                               {
                                   public string Value => condition.Name + contexts.Open("w").Value;
                               }

                               public sealed class Service(Func<Condition, Evaluator1> e1, Func<Condition, Evaluator2> e2, Func<Condition, Evaluator3> e3)
                               {
                                   public string Run() => $"{e1(new Condition("a")).Value} {e2(new Condition("b")).Value} {e3(new Condition("c")).Value}";
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.LocalFunctions, "On")
                                           .Bind().As(Lifetime.Singleton).To<ContextProvider>()
                                           .Bind().As(Lifetime.Singleton).To<ConfigurationProvider>()
                                           .Bind().As(Lifetime.Singleton).To<ProjectionService>()
                                           .Root<Service>("Root");
                               }

                               public static class Program
                               {
                                   public static void Main() => Console.WriteLine(new Composition().Root.Run());
                               }
                           }
                           """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["auP bvP cwP"], result);
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("new global::Sample.ContextProvider(")).ShouldBe(1, result.GeneratedCode);
    }

    private static string Create(int size, string lifetime, bool letFunc, string localFunctions)
    {
        var classes = new StringBuilder();
        var bindings = new StringBuilder();
        var last = size - 1;
        for (var i = 0; i < size; i++)
        {
            var isLast = i == last;
            var next = isLast ? "" : $", Service{i + 1} next";
            var nextProperty = isLast ? "" : $"public Service{i + 1} Next => next;";
            // The items of both delegates must get the same shared instance of the next level.
            var walk = isLast
                ? $"\"S{i}\""
                : $"\"S{i}\" + (ReferenceEquals(itemA.Next, itemB.Next) ? \"\" : \"!\") + itemA.Next.Walk()";
            classes.AppendLine(
                $$"""
                  public sealed class ItemA{{i}}(A a{{next}})
                  {
                      public A A => a;
                      {{nextProperty}}
                  }

                  public sealed class ItemB{{i}}(B b{{next}})
                  {
                      public B B => b;
                      {{nextProperty}}
                  }

                  public sealed class Service{{i}}(Func<A, ItemA{{i}}> openA, Func<B, ItemB{{i}}> openB)
                  {
                      public string Walk()
                      {
                          var itemA = openA(new A());
                          var itemB = openB(new B());
                          return {{walk}};
                      }
                  }
                  """);
            bindings.AppendLine($".Bind().As(Lifetime.{lifetime}).To<Service{i}>()");
        }

        // The argument of a delegate reaches only the item it creates.
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
                     public sealed class A;

                     public sealed class B;

                     {{classes}}

                     partial class Composition
                     {
                         private static void Setup() =>
                             DI.Setup(nameof(Composition))
                                 .Hint(Hint.LocalFunctions, "{{localFunctions}}")
                                 {{funcBinding}}
                                 {{bindings}}
                                 .Root<Service0>("Root");
                     }

                     public static class Program
                     {
                         public static void Main() => Console.WriteLine(new Composition().Root.Walk());
                     }
                 }
                 """;
    }
}
#endif
