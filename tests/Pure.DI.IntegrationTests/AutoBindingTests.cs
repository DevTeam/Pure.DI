namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class AutoBindingTests
{
    [Fact]
    public async Task ShouldSupportAutoBinding()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    class Dependency
    {
        public Dependency()
        {
            Console.WriteLine("Ctor1");
        }
    }

    interface IService {}
    class Service: IService
    {
        public Service(Dependency dep)
        {
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IService>().As(Lifetime.Transient).To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var service = new Composition().Service;                                
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Ctor1"), result);
    }
    
    [Fact]
    public async Task ShouldSupportAutoBindingWhenSeveralConstructors()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    class Dependency
    {
        internal Dependency(int id)
        {
            Console.WriteLine("Ctor2");
        }

        public Dependency()
        {
            Console.WriteLine("Ctor1");
        }
    }

    interface IService {}
    class Service: IService
    {
        public Service(Dependency dep)
        {
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IService>().As(Lifetime.Transient).To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var service = new Composition().Service;                                
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Ctor1"), result);
    }
}