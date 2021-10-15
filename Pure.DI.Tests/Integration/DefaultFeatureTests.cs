namespace Pure.DI.Tests.Integration
{
    using System.Collections.Generic;
    using Shouldly;
    using Xunit;

    public class DefaultFeatureTests
    {
        [Fact]
        public void ShouldSupportFunc()
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
                    internal CompositionRoot(Func<string> value) => Value = value();
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new [] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportFuncWithTag()
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
                            .Bind<string>().Tags(1).To(_ => ""abc"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot([Tag(1)] Func<string> value) => Value = value();
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportLazy()
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
                    internal CompositionRoot(Lazy<string> value) => Value = value.Value;        
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportThreadLocal()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using System.Threading;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .DependsOn(""Defaults"")
                            .Bind<string>().To(_ => ""abc"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(ThreadLocal<string> value) => Value = value.Value;        
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportTask()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using System.Threading.Tasks;

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
                    internal CompositionRoot(Task<string> task) 
                    {
                        task.RunSynchronously();
                        Value = task.Result;
                    }
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportTuple()
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
                            .Bind<int>().To(_ => 333)
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(Tuple<string, int> value) => Value = value.Item1 + value.Item2;
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc333" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportValueTuple()
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
                            .Bind<int>().To(_ => 333)
                            .Bind<long>().To(_ => 99)
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot((string, int, long) value) => Value = value.Item1 + value.Item2 + value.Item3;
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc33399" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportArray()
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
                            .Bind<string>().Tags(1).To(_ => ""1"")
                            .Bind<string>().To(_ => ""2"")                            
                            .Bind<string>().Tags(3).To(_ => ""3"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string[] value) => Value = String.Join(""."", value);
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "1.2.3" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportArrayWhenHasNoBindings()
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
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string[] value) => Value = String.Join(""."", value);
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "" }, generatedCode);
        }

        [Theory]
        [InlineData("IEnumerable")]
        [InlineData("ICollection")]
        [InlineData("IReadOnlyCollection")]
        [InlineData("IList")]
        [InlineData("List")]
        [InlineData("IReadOnlyList")]
        [InlineData("ISet")]
        [InlineData("HashSet")]
        [InlineData("SortedSet")]
        [InlineData("Queue")]
        [InlineData("Stack")]
        [InlineData("ImmutableArray")]
        [InlineData("IImmutableList")]
        [InlineData("ImmutableList")]
        [InlineData("IImmutableSet")]
        [InlineData("ImmutableHashSet")]
        [InlineData("ImmutableSortedSet")]
        [InlineData("IImmutableQueue")]
        [InlineData("ImmutableQueue")]
        [InlineData("IImmutableStack")]
        [InlineData("ImmutableStack")]
        public void ShouldSupportCollections(string collectionType)
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using System.Collections.Generic;
                using System.Collections.Immutable;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().Tags(1).To(_ => ""1"")
                            .Bind<string>().To(_ => ""2"")
                            .Bind<string>().Tags(3).To(_ => ""3"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(ICollection<string> value) => Value = String.Join(""."", value);
                }
            }".Replace("ICollection", collectionType).Run(out var generatedCode);

            // Then
            output.Count.ShouldBe(1, generatedCode);
            new HashSet<string>(output[0].Split(".")).SetEquals(new HashSet<string> { "1", "2", "3"} ).ShouldBeTrue(output[0]);
        }

        [Fact]
        public void ShouldSupportEnumerablesWhenHasNoBindings()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using System.Collections.Generic;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(IEnumerable<string> value) => Value = String.Join(""."", value);
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportSet()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using System.Collections.Generic;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().Tags(1).To(_ => ""1"")
                            .Bind<string>().To(_ => ""2"")                            
                            .Bind<string>().Tags(3).To(_ => ""3"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(ISet<string> value) => Value = String.Join(""."", value);
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "1.2.3" }, generatedCode);
        }
    }
}