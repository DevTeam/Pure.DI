namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the injection of the composition itself into its dependencies.
/// </summary>
public class CompositionInjectionTests
{
    [Fact]
    public async Task ShouldSupportCompositionInjection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {        
                                   public Dependency()
                                   {           
                                   }
                               }
                           
                               interface IService
                               {                    
                               }
                           
                               class Service: IService 
                               {
                                   public Service(Composition composition, IDependency dep)
                                   { 
                                       Console.WriteLine("Service creating");
                                   }    
                               }
                           
                               internal partial class Composition { }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;                              
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Service creating"], result);
    }

    [Fact]
    public async Task ShouldSupportCompositionInjectionInFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService 
                               {
                                   public Service(Composition composition)
                                   { 
                                       Console.WriteLine("Service creating");
                                   }    
                               }
                           
                               internal partial class Composition { }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To(ctx => {
                                               ctx.Inject<Composition>(out var composition);
                                               return new Service(composition);
                                           })
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;                              
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Service creating"], result);
    }

    [Fact]
    public async Task ShouldSupportCompositionInjectionInProperty()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Service 
                               {
                                   [Ordinal(0)]
                                   public Composition Comp { get; set; }
                               }
                           
                               internal partial class Composition { }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Comp != null);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }
}