namespace Pure.DI.Tests.Integration;

public class ArgsTests
{
    [Fact]
    public void ShouldSupportArg()
    {
        // Given
        
        // When
        var output = @"
            using System;
            using static Pure.DI.Lifetime;
            using Pure.DI;

            namespace Sample
            {
                // Let's create an abstraction

                interface IBox<out T> { T Content { get; } }

                enum State { Alive, Dead }

                // Here is our implementation

                class CardboardBox<T> : IBox<T>
                {
                    public CardboardBox(T content) => Content = content;

                    public T Content { get; }

                    public override string ToString() => $""[{ Content}]"";
                }

                class ShroedingersCat : ICat
                {
                    // Represents the superposition of the states
                    private readonly Lazy<State> _superposition;
                    private string _name;

                    public ShroedingersCat(Lazy<State> superposition, string name)
                    {
                        _superposition = superposition;
                        _name = name;
                    }

                    // The decoherence of the superposition at the time of observation via an irreversible process
                    public State State => _superposition.Value;

                    public override string ToString() => $""{State} cat {_name}"";
                }

                // Let's glue all together

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Arg<int>(""intSetting1"")
                            .Arg<int>(""intSetting2"")
                            .Arg<string>()
                            // Models a random subatomic event that may or may not occur
                            .Bind<Random>().As(Singleton).To<Random>()
                            // Represents a quantum superposition of 2 states: Alive or Dead
                            .Bind<State>().To(ctx => (State)ctx.Resolve<Random>().Next(2))
                            // Represents schrodinger's cat
                            .Bind<ICat>().To<ShroedingersCat>();

                        DI.Setup()
                            // Represents a cardboard box with any content
                            .Bind<IBox<TT>>().To<CardboardBox<TT>>()
                            // Composition Root
                            .Bind<CompositionRoot>().As(Singleton).To<CompositionRoot>();
                    }
                }

                // Time to open boxes!

                internal class CompositionRoot
                {
                    public readonly IBox<ICat> Value;
                    internal CompositionRoot(IBox<ICat> box) => Value = box;
                }
            }".Run(
            out var generatedCode,
            new RunOptions
            {
                Statements = "System.Console.WriteLine(Composer.Resolve<CompositionRoot>(22, 33, \"Abc\").Value);",
                AdditionalCode =
                {
                    "namespace Sample { interface ICat { State State { get; } } }"
                }
            });

        // Then
        (output.Contains("[Dead cat Abc]") || output.Contains("[Alive cat Abc]")).ShouldBeTrue(generatedCode);
    }
    
    [Fact]
    public void ShouldSupportFuncInCtor()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Arg<string>()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(Func<string> value) => Value = value();
                }
            }".Run(
            out var generatedCode,
            new RunOptions
            {
                Statements = "System.Console.WriteLine(Composer.Resolve<CompositionRoot>(\"Abc\").Value);"
            });

        // Then
        output.ShouldBe(new[] { "Abc" }, generatedCode);
    }
    
    [Fact]
    public void ShouldThrowInvalidOperationExceptionWhenDeferredResolveViaFunc()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Arg<string>()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    private Func<string> _value;
                    public string Value => _value();
                    internal CompositionRoot(Func<string> value) => _value = value;
                }
            }".Run(
            out var generatedCode,
            new RunOptions
            {
                Statements = "System.Console.WriteLine(Composer.Resolve<CompositionRoot>(\"Abc\").Value);"
            });

        // Then
        output.Any(i => i.Contains("System.InvalidOperationException")).ShouldBeTrue(generatedCode);
    }
}