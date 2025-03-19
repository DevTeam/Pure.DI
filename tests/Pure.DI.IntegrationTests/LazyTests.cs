namespace Pure.DI.IntegrationTests;

public class LazyTests
{
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
                                        // FormatCode = On
                                        DI.Setup(nameof(Composition))
                                            .Bind<IDependency>().To<Dependency>()
                                            .Bind<IDependency2>().To<Dependency2>()
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

    [Theory]
    [InlineData("PerResolve")]
    [InlineData("Singleton")]
    public async Task ShouldSupportLazyInjectionWhenFactory(string lifetime)
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
                                    public Dependency(Func<string, IDependency2> dependency2) { }
                                }
                           
                                public interface IDependency
                                {
                                }
                           
                                public class Dependency2 : IDependency2
                                {
                                    public Dependency2(IDependency dependency, string str) { }
                                }
                           
                                public interface IDependency2
                                {
                                }
                           
                                public interface IService
                                {
                                }
                           
                                public class Service : IService
                                {
                                    public Service(IDependency dependency)
                                    {
                                    }
                                }
                           
                                public partial class Composition
                                {
                                    public static void Setup() =>
                                        // FormatCode = On
                                        DI.Setup(nameof(Composition))
                                            .Bind<IDependency>().As(Lifetime.###).To(ctx => {
                                               ctx.Inject<Dependency>(out var dep);
                                               return dep;
                                           })
                                            .Bind<Func<string, IDependency2>>().As(Lifetime.###).To(ctx => new Func<string, IDependency2>(p => {
                                                ctx.Inject<IDependency>(out var dep2);
                                                return new Dependency2(dep2, p);
                                            }))
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
                           """.Replace("###", lifetime).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportLazyWhenCycle()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Tool
                               {
                                   public Tool(Lazy<View> view)
                                   {
                                   }
                               }
                               
                               abstract class ModelBase
                               {
                                   [Dependency]
                                   public Tool? Tool { private get; set; }
                               }
                               
                               class Model: ModelBase
                               {
                               }
                               
                               class View
                               {
                                    public View(Model model)
                                    {
                                    }
                               }
                           
                                partial class Composition
                                {
                                    public static void Setup() =>
                                        // FormatCode = On
                                        DI.Setup("Composition")
                                            // with the root "MyService"
                                            .Root<Owned<View>>("MyService");
                                }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.MyService;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
}