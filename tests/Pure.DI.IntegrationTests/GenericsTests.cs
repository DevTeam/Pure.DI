namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the generic type bindings and their resolution.
/// </summary>
public class GenericsTests
{

    [Fact]
    public async Task ShouldSupportCustomGenericTypeArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface TTMy { };
                               
                               internal interface IDependency<T> { }
                           
                               internal class Dependency<T> : IDependency<T> { }
                           
                               internal interface IService
                               {
                                   IDependency<int> IntDependency { get; }
                                   
                                   IDependency<string> StringDependency { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency<int> intDependency, IDependency<string> stringDependency)
                                   {
                                       IntDependency = intDependency;
                                       StringDependency = stringDependency;
                                   }
                                   
                                   public IDependency<int> IntDependency { get; }
                                   
                                   public IDependency<string> StringDependency { get; }
                               }    
                           
                               public class Program
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .GenericTypeArgument<TTMy>()
                                           .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;    
                                       Console.WriteLine(service.IntDependency.GetType());
                                       Console.WriteLine(service.StringDependency.GetType());     
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency`1[System.Int32]", "Sample.Dependency`1[System.String]"], result);
    }

    [Fact]
    public async Task ShouldSupportCustomGenericTypeArgumentAttribute()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
                               internal sealed class MyGenericAttribute : global::System.Attribute { }
                           
                               [MyGenericAttribute]
                               internal interface TTMy { };
                               
                               internal interface IDependency<T> { }
                           
                               internal class Dependency<T> : IDependency<T> { }
                           
                               internal interface IService
                               {
                                   IDependency<int> IntDependency { get; }
                                   
                                   IDependency<string> StringDependency { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency<int> intDependency, IDependency<string> stringDependency)
                                   {
                                       IntDependency = intDependency;
                                       StringDependency = stringDependency;
                                   }
                                   
                                   public IDependency<int> IntDependency { get; }
                                   
                                   public IDependency<string> StringDependency { get; }
                               }    
                           
                               public class Program
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .GenericTypeArgumentAttribute<MyGenericAttribute>()
                                           .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;    
                                       Console.WriteLine(service.IntDependency.GetType());
                                       Console.WriteLine(service.StringDependency.GetType());     
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency`1[System.Int32]", "Sample.Dependency`1[System.String]"], result);
    }
    [Fact]
    public async Task ShouldSupportGenerics()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency<T> { }
                           
                               internal class Dependency<T> : IDependency<T> { }
                           
                               internal interface IService
                               {
                                   IDependency<int> IntDependency { get; }
                                   
                                   IDependency<string> StringDependency { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency<int> intDependency, IDependency<string> stringDependency)
                                   {
                                       IntDependency = intDependency;
                                       StringDependency = stringDependency;
                                   }
                                   
                                   public IDependency<int> IntDependency { get; }
                                   
                                   public IDependency<string> StringDependency { get; }
                               }    
                           
                               public class Program
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency<TT>>().To<Dependency<TT>>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;    
                                       Console.WriteLine(service.IntDependency.GetType());
                                       Console.WriteLine(service.StringDependency.GetType());     
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency`1[System.Int32]", "Sample.Dependency`1[System.String]"], result);
    }
}