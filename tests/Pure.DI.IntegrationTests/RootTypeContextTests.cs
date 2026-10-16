namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests for issue #154: <c>ctx.RootType</c> and <c>ctx.RootName</c> should describe the composition
/// root which <c>Resolve</c> calls, not the generated lightweight root.
/// </summary>
public class RootTypeContextTests
{
    [Fact]
    public async Task ShouldReturnResolvedTypeForCtxRootTypeWhenUsingResolve()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class Composition
                               {
                                   private void Setup() => DI.Setup()
                                       .Bind().To(ctx => new Logger(ctx.RootType))
                                       .Root<Parent>();
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var parent = new Composition().Resolve<Parent>();
                                       Console.WriteLine(parent.Child.Logger.Type == typeof(Parent));
                                   }
                               }

                               public class Parent
                               {
                                   public Parent(Child child)
                                   {
                                       Child = child;
                                   }
                                   public Child Child { get; }
                               }

                               public class Child
                               {
                                   public Child(Logger logger)
                                   {
                                       Logger = logger;
                                   }
                                   public Logger Logger { get; }
                               }

                               public class Logger
                               {
                                   public Logger(Type type)
                                   {
                                       Type = type;
                                   }

                                   public Type Type { get; }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldReturnResolvedRootNameForCtxRootNameWhenUsingResolve()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class Composition
                               {
                                   private void Setup() => DI.Setup()
                                       .Bind().To(ctx => new Logger(ctx.RootName))
                                       .Root<Parent>();
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var parent = new Composition().Resolve<Parent>();
                                       Console.WriteLine(parent.Child.Logger.Name);
                                   }
                               }

                               public class Parent
                               {
                                   public Parent(Child child)
                                   {
                                       Child = child;
                                   }
                                   public Child Child { get; }
                               }

                               public class Child
                               {
                                   public Child(Logger logger)
                                   {
                                       Logger = logger;
                                   }
                                   public Logger Logger { get; }
                               }

                               public class Logger
                               {
                                   public Logger(string name)
                                   {
                                       Name = name;
                                   }

                                   public string Name { get; }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Parent"], result);
    }
}
