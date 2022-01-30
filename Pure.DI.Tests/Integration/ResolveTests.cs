namespace Pure.DI.Tests.Integration;

public class ResolveTests
{
    [Fact]
    public void ShouldResolveWhenGeneticResolveAndTagIsNull()
    {
        // Given
        const string? statements = "System.Console.WriteLine(Composer.Resolve<string>(null));";

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
                            .Bind<string>().To(_ => ""abc"");
                    }
                }
            }".Run(out var generatedCode, new RunOptions
        {
            Statements = statements
        });

        // Then
        output.ShouldBe(new[]
        {
            "abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldResolveWhenTagIsNull()
    {
        // Given
        const string? statements = "System.Console.WriteLine(Composer.Resolve(typeof(string), null));";

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
                            .Bind<string>().To(_ => ""abc"");
                    }
                }
            }".Run(out var generatedCode, new RunOptions
        {
            Statements = statements
        });

        // Then
        output.ShouldBe(new[]
        {
            "abc"
        }, generatedCode);
    }
}