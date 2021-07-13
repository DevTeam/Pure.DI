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
        public void ShouldSupportGenericSingleton()
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
                    internal CompositionRoot(Foo<int> value1, Foo<string> value2, Foo<int> value3, Foo<string> value4) => Value = value1 == value3 && value2 == value4;        
                }

                public class Foo<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo<TT>>().As(Pure.DI.Lifetime.Singleton).To<Foo<TT>>()
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
                    internal CompositionRoot(Foo value1, Foo value2, IFoo2 value3) => Value = value1.Foo2 == value2.Foo2 && value1.Foo2 == value3;        
                }

                public class Foo
                {
                    public IFoo2 Foo2;
                    public Foo(IFoo2 foo2) { Foo2 = foo2; }
                }

                public interface IFoo2 {}
                public class Foo2: IFoo2 { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().To<Foo>()
                            .Bind<IFoo2>().As(Pure.DI.Lifetime.Singleton).To<Foo2>()                            
                            .Bind<CompositionRoot>().To<CompositionRoot>();
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

        [Fact]
        public void ShouldSupportSingletonWhenFuncAndGeneric()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class Foo<T> { }

                public class CompositionRoot
                {
                    public readonly bool Value;
                    internal CompositionRoot(Func<Foo<string>> value1, Func<Foo<string>> value2) => Value = value1().Equals(value2());        
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo<TT>>().As(Pure.DI.Lifetime.Singleton).To<Foo<TT>>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "True" }, generatedCode);
        }
    }
}