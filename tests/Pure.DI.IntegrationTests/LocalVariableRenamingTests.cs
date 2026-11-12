namespace Pure.DI.IntegrationTests;

#pragma warning disable CA1861

// A local of a factory is renamed in the generated code, but a name that only looks like it keeps its text, see #197.
public class LocalVariableRenamingTests
{
    [Fact]
    public async Task ShouldKeepNamedArgumentWhenFactoryIsInSetupInAnotherFile()
    {
        // Given

        // When
        var result = await new[]
        {
            """
            using System;
            using Pure.DI;

            namespace Sample
            {
                public sealed class Options;

                public sealed class Service(string name, Options? options = null)
                {
                    public override string ToString() => $"{name} {options is not null}";
                }

                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .DependsOn("Services")
                        .Root<Service>("Root");
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
            """,
            """
            using Pure.DI;

            namespace Sample
            {
                static class Services
                {
                    private static void Setup() => DI.Setup("Services", CompositionKind.Internal)
                        .Bind<Options>().To<Options>()
                        .Bind<Service>().To(ctx =>
                        {
                            ctx.Inject(out Options options);
                            return new Service("service", options: options);
                        });
                }
            }
            """
        }.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["service True"], result);
    }

    [Fact]
    public async Task ShouldKeepNamedArgumentInSimpleFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public sealed class Options;

                               public sealed class Service(string name, Options? options = null)
                               {
                                   public override string ToString() => $"{name} {options is not null}";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Options>().To<Options>()
                                           .Bind<Service>().To((Options options) => new Service("service", options: options))
                                           .Root<Service>("Root");
                                   }
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
                           """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["service True"], result);
    }

    [Fact]
    public async Task ShouldKeepMemberNamesInObjectInitializerWhenFactoryIsInSetupInAnotherFile()
    {
        // Given

        // When
        var result = await new[]
        {
            """
            using System;
            using Pure.DI;

            namespace Sample
            {
                public sealed class Options;

                public sealed class Service
                {
                    public Options? options;

                    public object? info;

                    public override string ToString() => $"{options is not null} {info}";
                }

                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .DependsOn("Services")
                        .Root<Service>("Root");
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
            """,
            """
            using Pure.DI;

            namespace Sample
            {
                static class Services
                {
                    private static void Setup() => DI.Setup("Services", CompositionKind.Internal)
                        .Bind<Options>().To<Options>()
                        .Bind<Service>().To(ctx =>
                        {
                            ctx.Inject(out Options options);
                            var name = "abc";
                            return new Service { options = options, info = new { name, options = name.Length } };
                        });
                }
            }
            """
        }.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True { name = abc, options = 3 }"], result);
    }
}
