namespace Pure.DI.Tests.Integration
{
    using System.Linq;
    using Shouldly;
    using Xunit;

    public class ConstructorChoosingTests
    {
        [Fact]
        public void ShouldSelectCtor()
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
                    public readonly string Value;
                    internal CompositionRoot(Foo value) => Value = value.Value;
                }

                public class Foo1
                {
                    private Foo1() { }
                }

                public class Foo
                {
                    public string Value;

                    public Foo(Foo1 val) { Value = val.ToString(); }

                    public Foo(string str) { Value = str; }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().To<Foo>()
                            .Bind<string>().To(_ => ""abc"")
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }
        
        [Fact]
        public void ShouldSelectCtorWhenInternal()
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
                    public readonly string Value;
                    internal CompositionRoot(Foo value) => Value = value.Value;
                }

                public class Foo1
                {
                    private Foo1() { }
                }

                public class Foo
                {
                    public string Value;

                    public Foo(Foo1 val) { Value = val.ToString(); }

                    internal Foo(string str) { Value = str; }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().To<Foo>()
                            .Bind<string>().To(_ => ""abc"")
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }
    }
}