namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class GenericsTests
{
    [Fact]
    public async Task ShouldSupportGenerics()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    internal interface IDependency<T> { }

    internal class Dependency<T> : IDependency<T> { }

    internal interface IService
    {
        IDependency<int> IntDependency { get; }
        
        IDependency<string> StringDependency { get; }
    }

    internal class Service : IService
    {
        public Service(IDependency<int> intDependency, IDependency<string> stringDependency)
        {
            IntDependency = intDependency;
            StringDependency = stringDependency;
        }
        
        public IDependency<int> IntDependency { get; }
        
        public IDependency<string> StringDependency { get; }
    }    

    public class Program
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency<TT>>().To<Dependency<TT>>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Service");
        }

        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Service;    
            Console.WriteLine(service.IntDependency.GetType());
            Console.WriteLine(service.StringDependency.GetType());                            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.Dependency`1[System.Int32]", "Sample.Dependency`1[System.String]"), result);
    }
}