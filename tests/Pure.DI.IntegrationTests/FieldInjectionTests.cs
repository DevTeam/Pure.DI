namespace Pure.DI.IntegrationTests;

[Collection(nameof(IntegrationTestsCollectionDefinition))]
public class FieldInjectionTests
{
    [Fact]
    public async Task ShouldSupportFieldInjection()
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

        IDependency OtherDep0 { get; }

        IDependency OtherDep1 { get; }
    }

    class Service: IService 
    {
        private IDependency _dep1;
        [Ordinal(1)] public IDependency? _otherDep1;           
        [CustomOrdinal(0)] public IDependency? _otherDep0;

        public Service(IDependency dep)
        { 
            _dep1 = dep;           
        }

        public IDependency Dep => _dep1;
        public IDependency OtherDep0 => _otherDep0!;
        public IDependency OtherDep1 => _otherDep1!;                       
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
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
            var composition = new Composition();
            var service = composition.Service;
            Console.WriteLine(service.Dep != service.OtherDep0);
            Console.WriteLine(service.Dep != service.OtherDep1);                                         
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True", "True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportRequiredFieldInjection()
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

        IDependency OtherDep0 { get; }

        IDependency OtherDep1 { get; }
    }

    class Service: IService 
    {
        private IDependency _dep1;
        public required IDependency? _otherDep1;           
        [CustomOrdinal(0)] public IDependency? _otherDep0;

        public Service(IDependency dep)
        { 
            _dep1 = dep;           
        }

        public IDependency Dep => _dep1;
        public IDependency OtherDep0 => _otherDep0!;
        public IDependency OtherDep1 => _otherDep1!;                       
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
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
            var composition = new Composition();
            var service = composition.Service;
            Console.WriteLine(service.Dep != service.OtherDep0);
            Console.WriteLine(service.Dep != service.OtherDep1);                                         
        }
    }                
}
""".RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True", "True"), result);
    }
}