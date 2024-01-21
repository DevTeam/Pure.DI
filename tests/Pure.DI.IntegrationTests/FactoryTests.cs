namespace Pure.DI.IntegrationTests;

[Collection(nameof(IntegrationTestsCollectionDefinition))]
public class FactoryTests
{
    [Fact]
    public async Task ShouldSupportFactory()
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
            DI.Setup("Composition")
                .Bind<IDependency>().To(ctx => new Dependency())
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
            Console.WriteLine(service1.Dep != service2.Dep);                                            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFactoryWhenParenthesis()
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
            DI.Setup("Composition")
                .Bind<IDependency>().To((ctx) => new Dependency())
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
            Console.WriteLine(service1.Dep != service2.Dep);                                            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFactoryWhenGenericParenthesis()
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
            DI.Setup("Composition")
                .Bind<IDependency>().To<IDependency>((ctx) => new Dependency())
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
            Console.WriteLine(service1.Dep != service2.Dep);                                            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFactoryWhenBlock()
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
            DI.Setup("Composition")
                .Bind<IDependency>().To(ctx => 
                    { 
                        return new Dependency(); 
                    }
                )
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
            Console.WriteLine(service1.Dep != service2.Dep);                                            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFactoryWhenSingleton()
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
            DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Singleton).To(ctx => new Dependency())
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
            Console.WriteLine(service1.Dep == service2.Dep);                                            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFactoryWithInject()
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
            DI.Setup("Composition")
                .Bind<IDependency>().To(ctx => new Dependency())
                .Bind<IService>().To(ctx => {
                    IDependency @dependency1;
                    ctx.Inject<IDependency>(out @dependency1); new Service(@dependency1); ctx.Inject<IDependency>(out var dependency2); return new Service(dependency2);
                })    
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
            Console.WriteLine(service1.Dep != service2.Dep);                                            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFactoryWithSimpleInject()
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
                   DI.Setup("Composition")
                       .Bind<IDependency>().To(ctx => new Dependency())
                       .Bind<IService>().To(ctx => {
                           ctx.Inject<IDependency>(out var dependency);
                           return new Service(dependency);
                       })
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
                   Console.WriteLine(service1.Dep != service2.Dep);
               }
           }
        }
        """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFactoryWithInjectWhenBlocks()
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
                   DI.Setup("Composition")
                       .Bind<IDependency>().To(ctx => new Dependency())
                       .Bind<IService>().To(ctx => {
                           IDependency dependency1;
                           var rnd = new Random(1).Next(3);
                           if (rnd == 0)
                           {
                                ctx.Inject(out dependency1);
                           }
                           else
                           {
                               if (rnd == 1)
                               {
                                    ctx.Inject(out dependency1);
                                    return new Service(dependency1);
                               }
                               
                               ctx.Inject(out dependency1);
                           }
                           
                           return new Service(dependency1);
                       })
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
                   Console.WriteLine(service1.Dep != service2.Dep);
               }
           }
        }
        """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFactoryWithInjectWithTheSameValueName()
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

    class MyRoot
    {
        public MyRoot(IService service1, [Tag(2)] IService service2) { }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>().To(ctx => new Dependency())
                .Bind<IService>().To(ctx => {
                    ctx.Inject<IDependency>(out var dependency);
                    return new Service(dependency);
                })
                .Bind<IService>(2).To(ctx => {
                    IDependency dependency;
                    ctx.Inject<IDependency>(out dependency);
                    return new Service(dependency);
                })
                .Root<MyRoot>("Root");
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
    public async Task ShouldSupportFactoryWithParentTagInjection()
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
            Console.WriteLine("Created");           
        }

        public IDependency Dep { get; }
    }

    internal partial class Composition
    {
        private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)            
        {
            return value;                  
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnDependencyInjection = On
            DI.Setup("Composition")
                .Bind<IDependency>(123).To(ctx => new Dependency())
                .Bind<IService>(123).To(ctx => {
                    ctx.Inject<IDependency>(ctx.Tag, out var dependency);
                    return new Service(dependency);
                })    
                .Root<IService>("Service", 123);
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
""".RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Created"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFactoryWithInjectInFunc()
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
            DI.Setup("Composition")
                .Bind<IDependency>().To(ctx => new Dependency())
                .Bind<IService>().To(ctx => {
                    var func = new Func<IDependency>(() => {
                        ctx.Inject<IDependency>(out var dependency);
                        return dependency;
                    });
                    
                    var dependency2 = func();
                    return new Service(dependency2);
                })   
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
            Console.WriteLine(service1.Dep != service2.Dep);                                            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldReplaceContextTag()
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
            Console.WriteLine("Created");           
        }

        public IDependency Dep { get; }
    }

    internal partial class Composition
    {
        private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)            
        {
            return value;                  
        }
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // OnDependencyInjection = On
            DI.Setup("Composition")
                .Bind<IDependency>(123).To(ctx => new Dependency())
                .Bind<IService>(123).To(ctx => {
                    System.Console.WriteLine(ctx.Tag);
                    ctx.Inject<IDependency>(ctx.Tag, out var dependency);
                    return new Service(dependency);
                })    
                .Root<IService>("Service", 123);
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
""".RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("123", "Created"), result);
    }
    
    [Fact]
    public async Task ShouldSupportFactoryWithInjectWhenGenericTypeIsNotSpecifiedAsTypeArgument()
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
            DI.Setup("Composition")
                .Bind<IDependency>().To(ctx => new Dependency())
                .Bind<IService>().To(ctx => {
                    IDependency dependency1;
                    ctx.Inject(out dependency1); new Service(dependency1); ctx.Inject(out IDependency dependency2); return new Service(dependency2);
                })    
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
            Console.WriteLine(service1.Dep != service2.Dep);                                            
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("True"), result);
    }
    
    [Fact]
    public async Task ShouldSupportSingletonWithinFactoryAndFunc()
    {
        // Given

        // When
        var result = await """
       using System;
       using Pure.DI;

       namespace Sample
       {
           interface IDependency2 {}
       
           class Dependency2: IDependency2 { }
           
           interface IDependency3 {}
       
           class Dependency3
           {
               public Dependency3(IDependency2 depFactory)
               {
               }
           }
       
           interface IDependency {}
       
           class Dependency: IDependency
           {
               public Dependency(IDependency2 depFactory)
               {
               }
           }
       
           interface IService
           {
               IDependency Dep { get; }
           }
       
           class Service: IService
           {
               private Func<IDependency> _depFactory;
               public Service(Func<IDependency> depFactory, Dependency3 dep3)
               {
                   _depFactory = depFactory;
               }
       
               public IDependency Dep => _depFactory();
           }
       
           static class Setup
           {
               private static void SetupComposition()
               {
                   // FormatCode = On
                   DI.Setup("Composition")
                       .Bind<IDependency2>().As(Lifetime.Singleton).To<Dependency2>()
                       .Bind<IDependency>().To(ctx =>
                       {
                            ctx.Inject<IDependency2>(out var dep2);
                            return new Dependency(dep2);
                       })
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
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains(" = new Sample.Dependency2();")).ShouldBe(2);
    }
    
}