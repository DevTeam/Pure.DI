namespace Pure.DI.IntegrationTests;

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
}