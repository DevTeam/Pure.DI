namespace Pure.DI.Tests.Integration
{
    using System;
    using System.Linq;
    using Core;
    using Shouldly;
    using Xunit;

    public class CannotResolveTests
    {
        [Theory]
        [InlineData("int")]
        [InlineData("string")]
        public void ShouldThrowArgumentExceptionWhenCannotResolve(string type)
        {
            // Given

            // When
            var output =
                @"namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

                // ReSharper disable once ClassNeverInstantiated.Global
                public class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                   
                }    
            }".Replace("string", type).Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(DefaultValueStrategy.CannotResolveMessage) && i.Contains(nameof(ArgumentException))).ShouldBeTrue();
        }
    }
}