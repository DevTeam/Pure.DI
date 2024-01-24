namespace Pure.DI.IntegrationTests;

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
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>().Root<IService>("Service");
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
        result.StdOut.ShouldBe(ImmutableArray.Create("Service creating"), result);
    }
}