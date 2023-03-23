namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class BclInjectionTests
{
    [Theory]
    [InlineData("System.Collections.Generic.IList")]
    [InlineData("System.Collections.Immutable.ImmutableArray")]
    [InlineData("System.Collections.Generic.IReadOnlyList")]
    [InlineData("System.Collections.Generic.ISet")]
    [InlineData("System.Collections.Generic.HashSet")]
    [InlineData("System.Collections.Generic.SortedSet")]
    [InlineData("System.Collections.Generic.Queue")]
    [InlineData("System.Collections.Generic.Stack")]
    [InlineData("System.Collections.Immutable.IImmutableList")]
    [InlineData("System.Collections.Immutable.ImmutableList")]
    [InlineData("System.Collections.Immutable.IImmutableSet")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet")]
    [InlineData("System.Collections.Immutable.ImmutableSortedSet")]
    [InlineData("System.Collections.Immutable.IImmutableQueue")]
    [InlineData("System.Collections.Immutable.ImmutableQueue")]
    [InlineData("System.Collections.Immutable.IImmutableStack")]
    [InlineData("System.Collections.Immutable.ImmutableStack")]
    public async Task ShouldSupportCollectionInjection(string collectionType)
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency, IComparable<Dependency>, IComparable
    {        
        public Dependency()
        {
            Console.WriteLine("Dependency created");
        }

        public int CompareTo(Dependency other) => GetHashCode() - other.GetHashCode();
        
        public int CompareTo(object obj) => GetHashCode() - obj.GetHashCode();
    }

    interface IService
    {                    
    }

    class Service: IService 
    {
        public Service(###CollectionType###<IDependency> deps)
        { 
            Console.WriteLine("Service creating");            
        }                            
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>(1).To<Dependency>()
                .Bind<IDependency>(2).To<Dependency>()
                .Bind<IDependency>(3).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            try 
            {
                var composition = new Composition();
                var service = composition.Service;
            }                                     
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }                
        }
    }                
}
""".Replace("###CollectionType###", collectionType).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency created", "Dependency created", "Dependency created", "Service creating"), result.GeneratedCode);
    }
}