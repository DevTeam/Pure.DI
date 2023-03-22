namespace Pure.DI.IntegrationTests;

using System.Collections.Immutable;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class NamespaceTests
{
    [Fact]
    public async Task ShouldSupportNamespaceInCompositionName()
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
        private static void SetupComposition()
        {
            DI.Setup("MyNs.Abc.Composition")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()    
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new MyNs.Abc.Composition();
            Console.WriteLine(composition.GetType());                                           
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("MyNs.Abc.Composition"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldUseDefaultNamespaceDeclaration()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace My.Sample
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
            var composition = new My.Sample.Composition();
            Console.WriteLine(composition.GetType());                                           
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("My.Sample.Composition"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldUseDefaultFileScopedNamespaceDeclaration()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace My.Sample;

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
        var composition = new My.Sample.Composition();
        Console.WriteLine(composition.GetType());                                           
    }
}                

""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("My.Sample.Composition"), result.GeneratedCode);
    }
}