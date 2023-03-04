namespace Pure.DI.IntegrationTests;

using System.Collections.Immutable;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class NamespaceTests
{
    [Fact]
    public async Task ShouldSupportNamespaceInComposerName()
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

    interface IService
    {
        IDependency Dep { get; }
    }

    class Service: IService 
    {
        public Service(IDependency dep)
        { 
            Dep = dep;
        }

        public IDependency Dep { get; }
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("MyNs.Abc.Composer")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()    
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new MyNs.Abc.Composer();
            Console.WriteLine(composer.GetType());                                           
        }
    }                
}
""".RunAsync();

        // Then
        result.StdOut.ShouldBe(ImmutableArray.Create("MyNs.Abc.Composer"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldUseDefaultNamespace()
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

    interface IService
    {
        IDependency Dep { get; }
    }

    class Service: IService 
    {
        public Service(IDependency dep)
        { 
            Dep = dep;
        }

        public IDependency Dep { get; }
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()    
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Sample.Composer();
            Console.WriteLine(composer.GetType());                                           
        }
    }                
}
""".RunAsync();

        // Then
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.Composer"), result.GeneratedCode);
    }
}