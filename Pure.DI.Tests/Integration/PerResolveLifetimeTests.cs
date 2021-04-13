namespace Pure.DI.Tests.Integration
{
    using Shouldly;
    using Xunit;

    public class PerResolveLifetimeTests
    {
        [Fact]
        public void ShouldSupportPerResolve()
        {
            // Given
            const string? statements = "var root1 = Composer.Resolve<CompositionRoot>();" +
                                       "var root2 = Composer.Resolve<CompositionRoot>();" +
                                       "System.Console.WriteLine(root1.Value1 == root1.Value2);" +
                                       "System.Console.WriteLine(root2.Value1 == root2.Value2);" +
                                       "System.Console.WriteLine(root1.Value1 != root2.Value1);";

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;               

                public class CompositionRoot
                {
                    public readonly Foo Value1;
                    public readonly Foo Value2;
                    internal CompositionRoot(Foo value1, Foo value2)
                    {
                        Value1 = value1;
                        Value2 = value2;
                    }
                }

                public class Foo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().As(Pure.DI.Lifetime.PerResolve).To<Foo>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode, new RunOptions { Statements = statements });

            // Then
            output.ShouldBe(new[] { "True", "True", "True" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportPerResolveWhenValueType()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;               

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot(int value)
                    {
                        Value = value;
                    }
                }

                public class Foo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<int>().As(Pure.DI.Lifetime.PerResolve).To(_ => 10)
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "10" }, generatedCode);
        }
    }
}