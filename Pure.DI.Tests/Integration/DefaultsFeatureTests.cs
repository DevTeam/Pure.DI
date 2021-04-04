namespace Pure.DI.Tests.Integration
{
    using System;
    using System.IO;
    using System.Linq;
    using Shouldly;
    using Xunit;

    public class DefaultsFeatureTests
    {
        [Fact]
        public void ShouldSupportFunc()
        {
            // Given

            // When
            var output = (GetFeaturesCode() + @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    // Models a random subatomic event that may or may not occur
                    private static readonly Random Indeterminacy = new();

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
                    internal CompositionRoot(Func<string> value) => Value = value();        
                }
            }").Run(out var generatedCode);

            // Then
            output.ShouldBe(new [] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportLazy()
        {
            // Given

            // When
            var output = (GetFeaturesCode() + @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    // Models a random subatomic event that may or may not occur
                    private static readonly Random Indeterminacy = new();

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
                    internal CompositionRoot(Lazy<string> value) => Value = value.Value;        
                }
            }").Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportThreadLocal()
        {
            // Given

            // When
            var output = (GetFeaturesCode() + @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using System.Threading;

                static partial class Composer
                {
                    // Models a random subatomic event that may or may not occur
                    private static readonly Random Indeterminacy = new();

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
            }").Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportTask()
        {
            // Given

            // When
            var output = (GetFeaturesCode() + @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using System.Threading.Tasks;

                static partial class Composer
                {
                    // Models a random subatomic event that may or may not occur
                    private static readonly Random Indeterminacy = new();

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
                    internal CompositionRoot(Task<string> value) => Value = value.Result;        
                }
            }").Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }

        private string GetFeaturesCode()
        {
            var assembly = GetType().Assembly;
            var resourceName = assembly
                .GetManifestResourceNames()
                .Single(i => i.EndsWith("Defaults.cs"));

           using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Cannot read {resourceName}."));
           return reader.ReadToEnd();
        }
    }
}