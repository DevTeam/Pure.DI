namespace Pure.DI.IntegrationTests;

using Core;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class SetupTests
{
    [Fact]
    public async Task ShouldOverrideBinding()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<string>().To(_ => "Abc")
                .Bind<string>().To(_ => "Xyz")
                .Root<string>("Result");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);                                           
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeFalse(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Xyz"), result.GeneratedCode);
        result.Warnings.Count.ShouldBe(1);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding).ShouldBe(1);
    }
    
    [Fact]
    public async Task ShouldOverrideBindingWhenDependsOn()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    static class Setup
    {
        private static void SetupBaseComposition()
        {
            DI.Setup("BaseComposition")
                .Root<string>("Result")
                .Bind<string>().To(_ => "Abc");
        }

        private static void SetupComposition()
        {
            DI.Setup("Composition").DependsOn("BaseComposition")
                .Bind<string>().To(_ => "Xyz");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);                                           
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeFalse(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Xyz"), result.GeneratedCode);
        result.Warnings.Count.ShouldBe(1);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding).ShouldBe(1);
    }
    
    [Fact]
    public async Task ShouldOverrideBindingWhenDependency()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IService
    {
        string Dep { get; }
    }

    class Service: IService 
    {
        public Service(string dep) => Dep = dep;

        public string Dep { get; }
    }

    static class Setup
    {
        private static void SetupBaseComposition()
        {
            DI.Setup("BaseComposition")
                .Bind<IService>().To<Service>()
                .Bind<string>().To(_ => "Abc");
        }

        private static void SetupComposition()
        {
            DI.Setup("Composition").DependsOn("BaseComposition")
                .Root<IService>("Result")                
                .Bind<string>().To(_ => "Xyz");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result.Dep);                                           
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeFalse(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Xyz"), result.GeneratedCode);
        result.Warnings.Count.ShouldBe(1);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding).ShouldBe(1);
    }
    
    [Fact]
    public async Task ShouldSupportNestedUsing()
    {
        // Given

        // When
        var result = await """
using System;
using System.Collections.Generic;
namespace Sample.Models
{
    interface IDependency {}

    class Dependency: IDependency {}

    interface IService
    {        
    }

    class Service: IService 
    {
        public Service(IEnumerable<IDependency> deps)
        {             
        }        
    }
}

namespace Sample
{
    using Models;
    using Pure.DI;
    using static Pure.DI.Lifetime;

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("MyNs.Abc.Composition")
                .Bind<IDependency>().As(Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Bind<IEnumerable<IDependency>>().To(ctx => 
                {
                    ctx.Inject<IDependency>(out var dep1);
                    ctx.Inject<IDependency>(out var dep2);
                    return new List<IDependency> { dep1, dep2 };
                })    
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
}