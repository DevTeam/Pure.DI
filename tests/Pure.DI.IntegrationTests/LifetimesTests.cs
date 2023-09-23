namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class LifetimesTests
{
    [Fact]
    public async Task ShouldSupportTransient()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IService {}
    class Service: IService {}
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
            var composition = new Composition();
            Console.WriteLine(composition.Service != composition.Service);                    
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldAssumeTransientWhenNotSpecified()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IService {}
    class Service: IService {}
    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IService>().To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Service != composition.Service);                    
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportSingleton()
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

    internal interface IService
    {
        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    internal class Service : IService
    {
        public Service(Func<IDependency> dependency1, IDependency dependency2)
        {
            Dependency1 = dependency1();
            Dependency2 = dependency2;
        }

        public IDependency Dependency1 { get; }
                
        public IDependency Dependency2 { get; }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IDependency>("Dependency")
                .Root<IService>("Root");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service1 = composition.Root;
            var service2 = composition.Root;
            Console.WriteLine(service1.Dependency1 == service1.Dependency2);                    
            Console.WriteLine(service2.Dependency1 == service1.Dependency1);
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True", "True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportValueTypeSingleton()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency {}

    struct Dependency: IDependency 
    {
        public Dependency()
        {
            Console.WriteLine("Dependency Created");
        }
    }

    interface IService
    {
        IDependency Dep { get; }
    }

    class Service: IService 
    {
        public Service(IDependency dep) => Dep = dep;

        public IDependency Dep { get; }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service1 = composition.Service;                    
            var service2 = composition.Service;
            var service3 = composition.Service;
        }
    }                
}
""".RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency Created"), result);
    }
    
    [Fact]
    public async Task ShouldSupportSingletonComplex()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency1 {}

    class Dependency1: IDependency1 {}

    interface IDependency2
    {
        IDependency1 Dep1 { get; }
    }

    class Dependency2: IDependency2
    {
        public Dependency2(IDependency1 dep1) => Dep1 = dep1;

        public IDependency1 Dep1 { get; }
    }

    interface IDependency3
    {
        IDependency1 Dep1 { get; }

        IDependency2 Dep2 { get; }
    }

    class Dependency3: IDependency3
    {
        public Dependency3(IDependency1 dep1, IDependency2 dep2)
        { 
            Dep1 = dep1;
            Dep2 = dep2;
        }       

        public IDependency1 Dep1 { get; }

        public IDependency2 Dep2 { get; }
    }

    interface IService
    {
        IDependency1 Dep1 { get; }

        IDependency2 Dep2 { get; }

        IDependency3 Dep3 { get; }
    }

    class Service: IService 
    {
        public Service(IDependency1 dep1, IDependency2 dep2, IDependency3 dep3)
        {
            Dep1 = dep1;
            Dep2 = dep2;
            Dep3 = dep3;
        }

        public IDependency1 Dep1 { get; }

        public IDependency2 Dep2 { get; }

        public IDependency3 Dep3 { get; }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // FormatCode = On
            DI.Setup("Composition")
                .Bind<IDependency1>().To<Dependency1>()
                .Bind<IDependency2>().As(Lifetime.Singleton).To<Dependency2>()
                .Bind<IDependency3>().As(Lifetime.Singleton).To<Dependency3>()
                .Bind<IService>().To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service1 = composition.Service;
            var service2 = composition.Service;
            Console.WriteLine(service1 != service2);                    
            Console.WriteLine(service1.Dep1 != service2.Dep1);
            Console.WriteLine(service1.Dep2 == service2.Dep2);           
            Console.WriteLine(service1.Dep2.Dep1 == service2.Dep2.Dep1);
            Console.WriteLine(service1.Dep2.Dep1 != service1.Dep1);
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True", "True", "True", "True", "True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportSingletonComplexWithDisposable()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency1 {}

    class Dependency1: IDependency1 {}

    interface IDependency2
    {
        IDependency1 Dep1 { get; }
    }

    class Dependency2: IDependency2, IDisposable
    {
        public Dependency2(IDependency1 dep1)
        {
            Dep1 = dep1;
            Console.WriteLine("Dependency2");
        }

        public IDependency1 Dep1 { get; }

        public void Dispose()
        {
            Console.WriteLine("Dispose2");
        }
    }

    interface IDependency3
    {
        IDependency1 Dep1 { get; }

        IDependency2 Dep2 { get; }
    }

    class Dependency3: IDisposable, IDependency3
    {
        public Dependency3(IDependency1 dep1, IDependency2 dep2)
        { 
            Dep1 = dep1;
            Dep2 = dep2;
            Console.WriteLine("Dependency3");
        }       

        public IDependency1 Dep1 { get; }

        public IDependency2 Dep2 { get; }

        public void Dispose()
        {
            Console.WriteLine("Dispose3");
        }
    }

    interface IService
    {
        IDependency1 Dep1 { get; }

        IDependency2 Dep2 { get; }

        IDependency3 Dep3 { get; }
    }

    class Service: IService 
    {
        public Service(IDependency1 dep1, IDependency2 dep2, IDependency3 dep3)
        {
            Dep1 = dep1;
            Dep2 = dep2;
            Dep3 = dep3;
        }

        public IDependency1 Dep1 { get; }

        public IDependency2 Dep2 { get; }

        public IDependency3 Dep3 { get; }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency1>().To<Dependency1>()
                .Bind<IDependency2>().As(Lifetime.Singleton).To<Dependency2>()
                .Bind<IDependency3>().As(Lifetime.Singleton).To<Dependency3>()
                .Bind<IService>().To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            using var composition = new Composition();
            var service1 = composition.Service;
            var service2 = composition.Service;            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency2", "Dependency3", "Dispose3", "Dispose2"), result);
    }
    
    [Fact]
    public async Task ShouldSupportSimpleSingleton()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IService {}
    class Service: IService {}
    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IService>().As(Lifetime.Singleton).To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Service == composition.Service);                    
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportPerResolve()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency1 {}

    class Dependency1: IDependency1 {}

    interface IDependency2
    {
        IDependency1 Dep1 { get; }
    }

    class Dependency2: IDependency2
    {
        public Dependency2(IDependency1 dep1) => Dep1 = dep1;

        public IDependency1 Dep1 { get; }
    }

    interface IService
    {
        IDependency1 Dep1 { get; }

        IDependency2 Dep2 { get; }
        
        IDependency1 Dep3 { get; }
    }

    class Service: IService 
    {
        public Service(IDependency1 dep1, IDependency2 dep2, IDependency1 dep3)
        {
            Dep1 = dep1;
            Dep2 = dep2;
            Dep3 = dep3;
        }

        public IDependency1 Dep1 { get; }

        public IDependency2 Dep2 { get; }
        
        public IDependency1 Dep3 { get; }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency1>().As(Lifetime.PerResolve).To<Dependency1>()
                .Bind<IDependency2>().To<Dependency2>()
                .Bind<IService>().To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Service;
            Console.WriteLine(service.Dep1 == service.Dep2.Dep1);
            Console.WriteLine(service.Dep1 == service.Dep3);
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True", "True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportPreResolveWithinSingleton()
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

    interface ISin
    {
        IDependency Dep { get; }
    }

    class Sin: ISin
    {
        public Sin(IDependency dep)
        {
            Dep = dep;        
        }

        public IDependency Dep { get; }        
    }

    interface IService
    {        
        IDependency Dep { get; }

        ISin Sin { get; }
    }

    class Service: IService 
    {
        public Service(IDependency dep, ISin sin)
        {
            Dep = dep;
            Sin = sin;
        }

        public IDependency Dep { get; }

        public ISin Sin { get; }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
                .Bind<ISin>().As(Lifetime.Singleton).To<Sin>()
                .Bind<IService>().To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Service;
            var service2 = composition.Service;
            Console.WriteLine(service.Dep == service.Sin.Dep);        
            Console.WriteLine(service.Dep != service2.Dep);
            Console.WriteLine(service.Sin == service2.Sin);
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True", "True", "True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportPreResolveWithinFunc()
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

    interface ISin
    {
        IDependency Dep { get; }
    }

    class Sin: ISin
    {
        public Sin(IDependency dep)
        {
            Dep = dep;        
        }

        public IDependency Dep { get; }        
    }

    interface IService
    {        
        IDependency Dep { get; }

        ISin Sin { get; }
    }

    class Service: IService 
    {
        private Func<IDependency> _dep;
        public Service(Func<IDependency> dep, ISin sin)
        {
            _dep = dep;
            Sin = sin;
        }

        public IDependency Dep => _dep();

        public ISin Sin { get; }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // FormatCode = On
            DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
                .Bind<ISin>().To<Sin>()
                .Bind<IService>().To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Service;
            Console.WriteLine(service.Dep == service.Sin.Dep);            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportSingletonWithinFunc()
    {
        // Given

        // When
        var result = await """
            using System;
            using System.Collections.Generic;
            using Pure.DI;

            namespace Sample
            {
               interface IDep { }

                class Dep: IDep
                {
                    public Dep(Func<int> val) { }
                }

                class Service
                {
                    public Service(IEnumerable<IDep> deps) { }
                }

                static class Setup
                {
                   private static void SetupComposition()
                   {
                       DI.Setup("Composition")
                            .Hint(Hint.Resolve, "Off")
                            .Hint(Hint.FormatCode, "On")
                            .DefaultLifetime(Lifetime.Singleton)
                            .Bind<int>().To(_ => 99)
                            .Bind<IDep>().To<Dep>()
                            .Root<Service>("Service");
                   }
                }

                public class Program
                {
                   public static void Main()
                   {
                       var composition = new Composition();
                       var service = composition.Service;
                   }
                }
            }
        """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
}