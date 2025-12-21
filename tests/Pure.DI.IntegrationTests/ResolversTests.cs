namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the generation of the Resolve methods for the composition.
/// </summary>
public class ResolversTests
{
    [Fact]
    public async Task ShouldSupportResolvers()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>().Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Resolve<IService>());
                                       Console.WriteLine(composition.Resolve<IService>(null));
                                       Console.WriteLine(composition.Resolve(typeof(IService)));
                                       Console.WriteLine(composition.Resolve(typeof(IService), null));        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service", "Sample.Service", "Sample.Service", "Sample.Service"], result);
    }
    [Fact]
    public async Task ShouldSupportResolversWithTag()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>("myTag").To<Service>()
                                           .Root<IService>("Root", "myTag");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Resolve<IService>("myTag"));
                                       Console.WriteLine(composition.Resolve(typeof(IService), "myTag"));        
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service", "Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldThrowExceptionWhenTypeCannotBeResolved()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       try
                                       {
                                           composition.Resolve<int>();
                                       }
                                       catch(Exception ex)
                                       {
                                           Console.WriteLine(ex.GetType().Name);
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([nameof(CannotResolveException)], result);
    }
}