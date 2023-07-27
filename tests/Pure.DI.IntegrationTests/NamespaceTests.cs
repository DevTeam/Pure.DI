namespace Pure.DI.IntegrationTests;

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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("MyNs.Abc.Composition"), result);
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("My.Sample.Composition"), result);
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

""".RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("My.Sample.Composition"), result);
    }
    
    [Fact]
    public async Task ShouldSupportNamespaceForFuncWithGenericParams()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using System.Text.RegularExpressions;

namespace Sample
{
    interface IService
    {     
    }

    class Service: IService 
    {
        public Service(Func<string, Regex> regFactory)
        {             
            Console.WriteLine(regFactory(".+"));
        }        
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<Func<string, Regex>>().To(_ => new Func<string, Regex>(value => new Regex(value, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase)))
                .Bind<IService>().To<Service>()    
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service =  composition.Service;                                                      
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create(".+"), result);
    }
}