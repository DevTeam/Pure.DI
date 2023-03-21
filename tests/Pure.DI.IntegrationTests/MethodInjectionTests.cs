namespace Pure.DI.IntegrationTests;

using System.Collections.Immutable;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class MethodInjectionTests
{
    [Fact]
    public async Task ShouldSupportMethodInjection()
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
        [Ordinal(1)]
        internal void Initialize([Tag(374)] string depName)
        {
            Console.WriteLine($"Initialize {depName}");            
        }
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
        
        public void Run()
        {
            Console.WriteLine("Run");            
        }

        [Ordinal(7)]
        public void Activate()
        {
            Console.WriteLine("Activate");            
        }

        [Ordinal(1)]
        internal void Initialize(IDependency dep)
        {
            Console.WriteLine("Initialize");
            Console.WriteLine(dep != Dep);
        }
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()
                .Arg<string>("depName", 374)    
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer("dep");
            var service = composer.Service;                                           
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Initialize dep", "Initialize dep", "Initialize", "True", "Activate"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportMethodInjectionWhenCustomAttribute()
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
        [Ordinal(1)]
        internal void Initialize([Tag(374)] string depName)
        {
            Console.WriteLine($"Initialize {depName}");            
        }
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
        
        public void Run()
        {
            Console.WriteLine("Run");            
        }

        [Ordinal(7)]
        public void Activate()
        {
            Console.WriteLine("Activate");            
        }

        [CustomOrdinal(1)]
        internal void Initialize(IDependency dep)
        {
            Console.WriteLine("Initialize");
            Console.WriteLine(dep != Dep);
        }
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()
                .Arg<string>("depName", 374)
                .OrdinalAttribute<CustomOrdinalAttribute>()                    
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer("dep");
            var service = composer.Service;                                           
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Initialize dep", "Initialize dep", "Initialize", "True", "Activate"), result.GeneratedCode);
    }
}