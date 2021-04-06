namespace Pure.DI.Tests.Integration
{
    using System.Linq;
    using Core;
    using Shouldly;
    using Xunit;

    public class ErrorsTests
    {
        [Fact]
        public void ShouldDetectCircularDependency()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

                public class MyClass
                {
                    public MyClass(MyClass value) { }
                }

                public class CompositionRoot
                {
                    public readonly MyClass Value;
                    internal CompositionRoot(MyClass value) { }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<MyClass>().To<MyClass>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                   
                }    
            }".Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(Diagnostics.CircularDependency)).ShouldBeTrue(generatedCode);
        }

        [Fact]
        public void ShouldHandleCannotFindCtor()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

                public class MyClass
                {
                    private MyClass(MyClass value) { }
                }

                public class CompositionRoot
                {
                    public readonly MyClass Value;
                    internal CompositionRoot(MyClass value) { }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<MyClass>().To<MyClass>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                   
                }    
            }".Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(Diagnostics.CannotFindCtor)).ShouldBeTrue(generatedCode);
        }
    }
}