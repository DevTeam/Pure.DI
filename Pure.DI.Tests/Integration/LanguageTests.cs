namespace Pure.DI.Tests.Integration
{
    using Microsoft.CodeAnalysis.CSharp;
    using Shouldly;
    using Xunit;

    public class LanguageTests
    {
        [Fact]
        public void ShouldSupportCSharp7()
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
                            .Bind<string>().To(_ => ""abc"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(Func<string> value) { Value = value(); }        
                }
            }".Run(out var generatedCode, new RunOptions { LanguageVersion = LanguageVersion.CSharp7 });

            // Then
            output.ShouldBe(new [] { "abc" }, generatedCode);
        }
    }
}