namespace Pure.DI.Tests.Integration
{
    using System.Linq;
    using Core;
    using Shouldly;
    using Xunit;

    public class FallbackTests
    {
        [Fact]
        public void ShouldUseLastFallbackFactoryWhenSeveral()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

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
                            .Fallback(Fallback1)
                            .Bind<CompositionRoot>().To<CompositionRoot>()
                            .Fallback(Fallback2);
                    }

                    private static object Fallback1(Type type, object tag) => 1;
                    private static object Fallback2(Type type, object tag) => 2;
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldContain("2", generatedCode);
            output.Any(i => i.Contains(Diagnostics.CannotResolveDependencyWarning)).ShouldBeTrue(generatedCode);
        }

        [Fact]
        public void ShouldUseLastFallbackFactoryWhenLambda()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

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
                            .Fallback((type, tag) => Fallback1(type, tag))
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }

                    private static object Fallback1(Type type, object tag) => 1;
                }                
            }".Run(out var generatedCode);

            // Then
            output.ShouldContain("1", generatedCode);
            output.Any(i => i.Contains(Diagnostics.CannotResolveDependencyWarning)).ShouldBeTrue(generatedCode);

        }

        [Fact]
        public void ShouldUseLastFallbackFactoryWhenLambdaWithRevertedArgs()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

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
                            .Fallback((type, tag) => Fallback1(tag, type))
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }

                    private static object Fallback1(object tag, Type type) => 1;
                }                
            }".Run(out var generatedCode);

            // Then
            output.ShouldContain("1", generatedCode);
            output.Any(i => i.Contains(Diagnostics.CannotResolveDependencyWarning)).ShouldBeTrue(generatedCode);

        }

        [Fact]
        public void ShouldUseFallbackFactoryWhenCannotResolve()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

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
                            .Fallback(Fallback1)       
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }

                    private static object Fallback1(Type type, object tag) => 1;                    
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldContain("1", generatedCode);
            output.Any(i => i.Contains(Diagnostics.CannotResolveDependencyWarning)).ShouldBeTrue(generatedCode);

        }
    }
}