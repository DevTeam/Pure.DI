namespace Pure.DI.Tests.Integration
{
    using Shouldly;
    using Xunit;

    public class SingletonTests
    {
        [Fact]
        public void ShouldSupportSingleton()
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
                    public readonly bool Value;
                    internal CompositionRoot(Foo value1, Foo value2) => Value = value1 == value2;        
                }

                public class Foo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().As(Pure.DI.Lifetime.Singleton).To<Foo>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "True" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportSingletonWhenNested()
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
                    public readonly bool Value;
                    internal CompositionRoot(Foo value1, Foo value2, Foo2 value3) => Value = value1.Foo2 == value2.Foo2 && value1.Foo2 == value3;        
                }

                public class Foo
                {
                    public Foo2 Foo2;
                    public Foo(Foo2 foo2) { Foo2 = foo2; }
                }

                public class Foo2 { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo2>().As(Pure.DI.Lifetime.Singleton).To<Foo2>()
                            .Bind<Foo>().To<Foo>()
                            .Bind<CompositionRoot>().As(Pure.DI.Lifetime.Singleton).To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "True" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportSingletonWhenFactory()
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
                    internal CompositionRoot(int value1, int value2) => Value = value1 + value2;        
                }

                internal static partial class Composer
                {
                    private static int val = 1;

                    static Composer()
                    {
                        DI.Setup()
                            .Bind<int>().As(Pure.DI.Lifetime.Singleton).To(_ => ++val)
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new []{"4"}, generatedCode);
        }

        [Fact]
        public void ShouldSupportSingletonForSeveralDependencies()
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
                    public readonly bool Value;
                    internal CompositionRoot(IFoo value1, Foo value2) => Value = value1 == value2;        
                }


                public interface IFoo { }

                public class Foo: IFoo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFoo>().Bind<Foo>().As(Pure.DI.Lifetime.Singleton).To<Foo>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "True" }, generatedCode);
        }
    }
}