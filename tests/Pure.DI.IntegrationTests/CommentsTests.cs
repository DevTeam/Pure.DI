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
    public async Task ShouldSupportCommentsOnBindings()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency: IDependency {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           // This is a dependency
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<IDependency>("Root");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains("/// This is a dependency").ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldSupportXmlComments()
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
                                           /// <summary>
                                           /// Service Name Root
                                           /// </summary>
                                           .Root<string>("ServiceName");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains("/// <summary>").ShouldBeTrue();
        result.GeneratedCode.Contains("/// Service Name Root").ShouldBeTrue();
    }
}