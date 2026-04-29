namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the generation of comments in the generated code.
/// </summary>
public class CommentsTests
{
    [Fact]
    public async Task ShouldSupportComments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           // My service name line1
                                           // My service name line2
                                           .Arg<string>("serviceName")
                                           // My root line 1           
                                           // My root line 2
                                           .Root<string>("ServiceName");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.ServiceName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains("/// My service name line1").ShouldBeTrue();
        result.GeneratedCode.Contains("/// My service name line2").ShouldBeTrue();
        result.GeneratedCode.Contains("/// My root line 1").ShouldBeTrue();
        result.GeneratedCode.Contains("/// My root line 2").ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldSupportXmlComments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           #pragma warning disable CS1587

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       /// <summary>
                                       /// Sample Composition
                                       /// </summary>
                                       DI.Setup("Composition")
                                           .Arg<string>("serviceName")
                                           /// <summary>
                                           /// Service Name Root
                                           /// </summary>
                                           .Root<string>("ServiceName");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.ServiceName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        var expectedClassComment = new[]
        {
            "/// <summary>",
            "/// Sample Composition",
            "/// </summary>"
        };
        var expectedRootComment = new[]
        {
            "/// <summary>",
            "/// Service Name Root",
            "/// </summary>"
        };

        var generatedLines = result.GeneratedCode
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => line.TrimStart())
            .ToList();
        var classLineIndex = generatedLines.FindIndex(line => line.Contains("partial class Composition", StringComparison.Ordinal));
        var rootLineIndex = generatedLines.FindIndex(line => line.Contains("public string ServiceName", StringComparison.Ordinal));

        classLineIndex.ShouldBeGreaterThan(0, result);
        GetLastXmlCommentBefore(classLineIndex).ShouldBe(expectedClassComment, result);

        rootLineIndex.ShouldBeGreaterThan(0, result);
        GetLastXmlCommentBefore(rootLineIndex).ShouldBe(expectedRootComment, result);
        return;

        string[] GetLastXmlCommentBefore(int lineIndex) =>
            generatedLines
                .Take(lineIndex)
                .Reverse()
                .SkipWhile(line => !line.StartsWith("///", StringComparison.Ordinal))
                .TakeWhile(line => line.StartsWith("///", StringComparison.Ordinal))
                .Reverse()
                .ToArray();
    }
}
