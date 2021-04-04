namespace Pure.DI.Tests.Integration
{
    using Shouldly;
    using Xunit;

    public class SetupTests
    {
        [Theory]
        [InlineData("partial class")]
        [InlineData("static class")]
        [InlineData("partial struct")]
        [InlineData("struct")]
        [InlineData("partial record")]
        [InlineData("record")]
        public void ShouldChangeComposerNameAddingDIWhenItIsNotPossibleToMakeStaticPartialClass(string classDefinition)
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
                    // Models a random subatomic event that may or may not occur
                    private static readonly Random Indeterminacy = new();

                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""abc"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }
            }".Replace("static partial class", classDefinition).Run(out var generatedCode, "ComposerDI");

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldSetupForNestedClass()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public partial class Foo
                {
                    public static partial class Composer
                    {
                        // Models a random subatomic event that may or may not occur
                        private static readonly Random Indeterminacy = new();

                        static Composer()
                        {
                            DI.Setup(""Resolver"")
                                .Bind<string>().To(_ => ""abc"")
                                // Composition Root
                                .Bind<CompositionRoot>().To<CompositionRoot>();
                        }
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }                
            }".Run(out var generatedCode, "Resolver");

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldOverrideBinding()
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
                    // Models a random subatomic event that may or may not occur
                    private static readonly Random Indeterminacy = new();

                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""abc"")
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new [] { "xyz" }, generatedCode);
        }
    }
}