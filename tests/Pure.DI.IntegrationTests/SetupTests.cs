namespace Pure.DI.IntegrationTests;

using Core;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class SetupTests
{
    [Fact]
    public async Task ShouldGenerateCompositionWhenHasNoAnyRoots()
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
                .Bind<string>().To(_ => "Abc");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();                                                       
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Warnings.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task ShouldShowErrorWhenDuplicatedRoots()
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
                .Root<string>("Root1")
                .Root<string>("Root2")
                .Root<string>("Root3");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();                                                       
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(2);
    }
    
    [Fact]
    public async Task ShouldShowErrorWhenCannotResolveRoot()
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
            }

            class Service: IService 
            {
            }
       
            static class Setup
            {
               private static void SetupComposition()
               {
                   DI.Setup("Composition")
                       .Bind<IService>(1).To<Service>()
                       .Root<IService>("MyRoot1");
               }
            }

            public class Program
            {
               public static void Main()
               {
                   var composition = new Composition();
               }
            }
       }
       """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors
            .Count(i => 
                i.Id == LogId.ErrorUnableToResolve
                && i.Lines.FirstOrDefault() == "Unable to resolve \"Sample.IService\" in Sample.IService() MyRoot1.")
            .ShouldBe(1);
    }
    
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
        result.Success.ShouldBeFalse(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Xyz"), result);
        result.Warnings.Count.ShouldBe(1);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding).ShouldBe(1);
    }
    
    [Fact]
    public async Task ShouldOverrideGlobalBindingWithoutWarning()
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
                .Bind<global::System.Collections.Generic.IComparer<TT>>().To(_ => global::System.Collections.Generic.Comparer<TT>.Default)                           
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
        result.Success.ShouldBeTrue(result);
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
        result.Success.ShouldBeFalse(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Xyz"), result);
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
            DI.Setup("BaseComposition", CompositionKind.Internal)
                .Bind<IService>().To<Service>()
                .Bind<string>().To(_ => "Abc");
        }

        private static void SetupComposition()
        {
            DI.Setup("Composition").DependsOn("BaseComposition")
                .Bind<string>().To(_ => "Xyz")
                .Root<IService>("Result");
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
        result.Success.ShouldBeFalse(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Xyz"), result);
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
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("MyNs.Abc.Composition"), result);
    }
    
    [Fact]
    public async Task ShouldSupportNonAbstractRoots()
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

    internal interface IService { }

    internal class Service : IService
    {
        public Service(IDependency dependency)
        {
        }
    }

    internal class OtherService : IService
    {
    }

    static class Setup
    {
        private static void SetupComposition() =>
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>("Other").To<OtherService>()
                .Bind<IService>().To<Service>()
                .Root<Service>("Service")
                .Root<OtherService>("OtherRoot", "Other");
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Service);
            Console.WriteLine(composition.OtherRoot);                                           
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Sample.Service", "Sample.OtherService"), result);
    }
    
    [Fact]
    public async Task ShouldSupportCombinedSetup()
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

    internal interface IService { }

    internal class Service : IService
    {
        public Service(IDependency dependency)
        {
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IDependency>(1).To<Dependency>()
                .Bind<IDependency>(2).To<Dependency>()
                .Bind<IDependency>(3).To<Dependency>()
                .Bind<IDependency>(4).To<Dependency>()
                .Bind<IDependency>(5).To<Dependency>()
                .Bind<IDependency>(6).To<Dependency>()
                .Bind<IDependency>(7).To<Dependency>();

            DI.Setup("Composition")
                .Bind<IService>().To<Service>().Root<IService>();
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();                                                       
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
    
    [Fact]
    public async Task ShouldGenerateSpecifiedRootType()
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

    internal interface IService1 { }

    internal interface IService2 { }

    internal interface IService3 { }

    internal class Service : IService1, IService2, IService3
    {
        public Service(IDependency dependency)
        {
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService1>().Bind<IService2>().Bind<IService3>().To<Service>()
                .Root<IService2>("Service");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();  
            IService2 service2 = composition.Service;                                                  
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
    
    [Fact]
    public async Task ShouldSupportMultRoots()
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

    internal interface IService1 { }

    internal interface IService2 { }

    internal interface IService3 { }

    internal class Service : IService1, IService2, IService3
    {
        public Service(IDependency dependency)
        {
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService1>().Bind<IService2>().Bind<IService3>().To<Service>()
                .Root<IService2>("Service2")
                .Root<IService3>("Service3");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();  
            IService2 service2 = composition.Service2;
            IService3 service3 = composition.Service3;                                                  
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
    
    [Theory]
    [InlineData("Func")]
    [InlineData("Lazy")]
    public async Task ShouldSupportLazyInjection(string lazyType)
    {
        // Given

        // When
        var result = await """
           using System;
           using Pure.DI;

           namespace Sample
           {
           
                using Pure.DI;

                public class Dependency : IDependency
                {
                    public Dependency(Func<IDependency2> dependency2) { }
                }

                public interface IDependency    
                {
                }

                public class Dependency2 : IDependency2
                {
                    public Dependency2(IDependency dependency) { }
                }

                public interface IDependency2   
                {
                }

                public interface IService
                {
                }

                public class Service : IService
                {
                    public Service(IDependency2 dependency2)
                    {
                    }
                }
           
                public partial class Composition
                {
                    public static void Setup() =>
                        DI.Setup(nameof(Composition))
                            .Bind<IDependency>().To<Dependency>().Root<IDependency>("Dependency")
                            .Bind<IDependency2>().To<Dependency2>().Root<IDependency2>("Dependency2")
                            .Bind<IService>().To<Service>().Root<IService>("Service");
                }
           
               public class Program
               {
                   public static void Main()
                   {
                       var composition = new Composition();
                       IService service = composition.Service;
                   }
               }
           }
           """.Replace("Func<", lazyType + "<").RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
}