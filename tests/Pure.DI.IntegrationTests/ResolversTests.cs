namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
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
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.Service", "Sample.Service", "Sample.Service", "Sample.Service"), result);
    }
}