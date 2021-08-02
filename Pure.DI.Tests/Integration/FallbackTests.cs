namespace Pure.DI.Tests.Integration
{
    using System.Linq;
    using Core;
    using Shouldly;
    using Xunit;

    public class FallbackTests
    {
        [Fact]
        public void ShouldUseFallback()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

                public class Fallback: IFallback
                {
                    public object Resolve(Type type, object tag) => 1;
                }

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot(int value) => Value = value;
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFallback>().To<Fallback>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldContain("1", generatedCode);
            output.Any(i => i.Contains(Diagnostics.Warning.CannotResolve)).ShouldBeTrue(generatedCode);
        }
    }
}