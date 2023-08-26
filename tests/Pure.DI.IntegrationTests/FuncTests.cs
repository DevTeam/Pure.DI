namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class FuncTests
{
    [Fact]
    public async Task ShouldSupportFuncForTransientDependencies()
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
        private Func<IDependency> _depFactory;
        public Service(Func<IDependency> depFactory)
        { 
            _depFactory = depFactory;           
        }

        public IDependency Dep => _depFactory();
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
            var composition = new Composition();
            var service = composition.Service;
            Console.WriteLine(service.Dep != service.Dep);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFuncForSingletonDependencies()
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
        private Func<IDependency> _depFactory;
        public Service(Func<IDependency> depFactory)
        { 
            _depFactory = depFactory;           
        }

        public IDependency Dep => _depFactory();
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
            var service = composition.Service;
            Console.WriteLine(service.Dep == service.Dep);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFuncForPerResolveDependencies()
    {
        // Given

        // When
        var result = await """
using System;
using System.Collections.Generic;
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
        private Func<IDependency> _depFactory;

        public Service(Func<IDependency> depFactory)
        { 
            _depFactory = depFactory;           
        }

        public IDependency Dep => _depFactory();
    }
    
    class Consumer 
    {
        private Func<IService> _serviceFactory;
        private Func<IDependency> _depFactory;

        public Consumer(Func<IService> serviceFactory, Func<IDependency> depFactory)
        { 
            _serviceFactory = serviceFactory;
            _depFactory = depFactory;           
        }

        public IService Service => _serviceFactory();
        public IDependency Dep => _depFactory();
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // FormatCode = On
            DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
                .Bind<IService>().To<Service>()    
                .Root<Consumer>("Consumer");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var consumer = composition.Consumer;
            var service = consumer.Service;
            Console.WriteLine(service.Dep == service.Dep);
            Console.WriteLine(consumer.Dep == service.Dep);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True", "True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFuncWithTag()
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
        private Func<IDependency> _depFactory;
        public Service([Tag("Abc")] Func<IDependency> depFactory)
        { 
            _depFactory = depFactory;           
        }

        public IDependency Dep => _depFactory();
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>("Abc").To<Dependency>()
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
            Console.WriteLine(service.Dep != service.Dep);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFuncOfFuncOfFunc()
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
        private Func<IDependency> _depFactory;
        public Service(Func<Func<Func<IDependency>>> depFactory)
        { 
            _depFactory = depFactory()();           
        }

        public IDependency Dep => _depFactory();
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
            var composition = new Composition();
            var service = composition.Service;
            Console.WriteLine(service.Dep != service.Dep);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFuncOfIEnumerable()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using System.Linq;
using System.Collections.Generic;

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
        private System.Collections.Generic.IEnumerable<IDependency> _depFactory;
        public Service(Func<Func<System.Collections.Generic.IEnumerable<IDependency>>> depFactory)
        { 
            _depFactory = depFactory()();           
        }

        public IDependency Dep => _depFactory.First();
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
            var composition = new Composition();
            var service = composition.Service;
            Console.WriteLine(service.Dep != service.Dep);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFuncOfArray()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using System.Linq;
using System.Collections.Generic;

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
        private IDependency[] _depFactory;
        public Service(Func<Func<IDependency[]>> depFactory)
        { 
            _depFactory = depFactory()();           
        }

        public IDependency Dep => _depFactory[0];
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
            var composition = new Composition();                                                       
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
    
    [Fact]
    public async Task ShouldSupportFuncOfTuple()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using System.Linq;
using System.Collections.Generic;

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
        private System.Collections.Generic.IEnumerable<IDependency> _depFactory;
        public Service(Func<(System.Collections.Generic.IEnumerable<IDependency>, System.Collections.Generic.IEnumerable<IDependency>)> depFactory)
        { 
            _depFactory = depFactory().Item1;           
        }

        public IDependency Dep => _depFactory.First();
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
            var composition = new Composition();
            var service = composition.Service;
            Console.WriteLine(service.Dep != service.Dep);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFuncWithArg()
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
        private string _name;

        public Dependency(string name)
        {
            _name = name;
        }

        public override string ToString() => _name;
    }

    interface IService
    {
        IDependency Dep { get; }        
    }

    class Service: IService 
    {
        private Func<string, IDependency> _depFactory;
        public Service([Tag(typeof(string))] Func<string, IDependency> depFactory)
        { 
            _depFactory = depFactory;           
        }

        public IDependency Dep => _depFactory("Xyz");
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<System.Func<string, IDependency>>(typeof(string)).To(ctx => new System.Func<string, IDependency>(i => 
                {
                    ctx.Inject<string>(out var prefix);
                    return new Dependency(prefix + i);
                }))
                .Bind<IService>().To<Service>()
                .Arg<string>("prefix")    
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition("Abc");
            var service = composition.Service;
            Console.WriteLine(service.Dep.ToString());                               
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("AbcXyz"), result);
    }
}