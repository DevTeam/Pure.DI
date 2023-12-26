namespace Pure.DI.IntegrationTests;

[Collection(nameof(IntegrationTestsCollectionDefinition))]
public class TuplesTests
{
    [Fact]
    public async Task ShouldSupportTuples()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    internal interface IDependency { }

    internal class Dependency : IDependency { }

    internal readonly record struct Point(int X, int Y);

    internal interface IService
    {
        IDependency Dependency { get; }
    }

    internal class Service : IService
    {
        private readonly Lazy<IDependency> _dependency;

        public Service(Lazy<IDependency> dependency)
        {
            _dependency = dependency;
        }

        public IDependency Dependency => _dependency.Value;
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>()
                .Bind<Point>().To(_ => new Point(7, 9))
                .Bind<IService>().To<Service>()
                .Root<(IService Service, Point Point, IDependency Dependency)>("Root");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var tuple = composition.Root;
            Console.WriteLine(tuple);            
        }
    }                
}
""".RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("(Sample.Service, Point { X = 7, Y = 9 }, Sample.Dependency)"), result);
    }
}