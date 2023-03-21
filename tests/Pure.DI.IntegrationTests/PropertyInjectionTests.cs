namespace Pure.DI.IntegrationTests;

using System.Collections.Immutable;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class PropertyInjectionTests
{
    [Fact]
    public async Task ShouldSupportPropertyInjection()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    internal class CustomOrdinalAttribute : Attribute
    {
        public readonly int Ordinal;

        public CustomOrdinalAttribute(int ordinal)
        {
            Ordinal = ordinal;
        }
    }

    interface IDependency {}

    class Dependency: IDependency
    {        
    }

    interface IService
    {
        IDependency Dep { get; }        
    }

    class Service: IService 
    {
        private IDependency _dep;
        public Service(IDependency dep)
        { 
            _dep = dep;           
        }

        public IDependency Dep => _dep;
        
        [Ordinal(1)]
        public IDependency OtherDep1
        {
            set
            {
                Console.WriteLine("OtherDep1");
                Console.WriteLine(value != Dep);
            }
        }        

        [CustomOrdinal(0)]
        public IDependency OtherDep0
        {
            set
            {
                Console.WriteLine("OtherDep0");
                Console.WriteLine(value != Dep);
            }
        }
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Service")
                .OrdinalAttribute<CustomOrdinalAttribute>();
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer();
            var service = composer.Service;                                           
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("OtherDep0", "True", "OtherDep1", "True"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportInitPropertyInjection()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    internal class CustomOrdinalAttribute : Attribute
    {
        public readonly int Ordinal;

        public CustomOrdinalAttribute(int ordinal)
        {
            Ordinal = ordinal;
        }
    }

    interface IDependency {}

    class Dependency: IDependency
    {        
    }

    interface IService
    {
        IDependency Dep { get; }        
    }

    class Service: IService 
    {
        private IDependency _dep;
        public Service(IDependency dep)
        { 
            _dep = dep;           
        }

        public IDependency Dep => _dep;
        
        [CustomOrdinal(1)]
        public IDependency OtherDep1
        {
            init
            {
                Console.WriteLine("OtherDep1");
                Console.WriteLine(value != Dep);
            }
        }        

        [Ordinal(0)]
        public IDependency OtherDep0
        {
            set
            {
                Console.WriteLine("OtherDep0");
                Console.WriteLine(value != Dep);
            }
        }
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Service")
                .OrdinalAttribute<CustomOrdinalAttribute>();
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer();
            var service = composer.Service;                                           
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("OtherDep1", "True", "OtherDep0", "True"), result.GeneratedCode);
    }
}