namespace Pure.DI.IntegrationTests;

using System.Collections.Immutable;

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
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IService>().As(Lifetime.Transient).To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer();
            Console.WriteLine(composer.Service != composer.Service);                    
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result.GeneratedCode);
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
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IService>().To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer();
            Console.WriteLine(composer.Service != composer.Service);                    
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result.GeneratedCode);
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
    interface IDependency {}

    class Dependency: IDependency {}

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
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer();
            Console.WriteLine(composer.Service != composer.Service);                    
            Console.WriteLine(composer.Service.Dep == composer.Service.Dep);
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("True", "True"), result.GeneratedCode);
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
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer();
            var service1 = composer.Service;                    
            var service2 = composer.Service;
            var service3 = composer.Service;
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency Created"), result.GeneratedCode);
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
        private static void SetupComposer()
        {
            DI.Setup("Composer")
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
            var composer = new Composer();
            var service1 = composer.Service;
            var service2 = composer.Service;
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
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("True", "True", "True", "True", "True"), result.GeneratedCode);
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
        private static void SetupComposer()
        {
            DI.Setup("Composer")
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
            using var composer = new Composer();
            var service1 = composer.Service;
            var service2 = composer.Service;            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency2", "Dependency3", "Dispose3", "Dispose2"), result.GeneratedCode);
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
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IService>().As(Lifetime.Singleton).To<Service>()               
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer();
            Console.WriteLine(composer.Service == composer.Service);                    
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result.GeneratedCode);
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
    }

    class Service: IService 
    {
        public Service(IDependency1 dep1, IDependency2 dep2)
        {
            Dep1 = dep1;
            Dep2 = dep2;
        }

        public IDependency1 Dep1 { get; }

        public IDependency2 Dep2 { get; }
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
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
            var composer = new Composer();
            var service = composer.Service;
            Console.WriteLine(service.Dep1 == service.Dep2.Dep1);
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result.GeneratedCode);
    }
}