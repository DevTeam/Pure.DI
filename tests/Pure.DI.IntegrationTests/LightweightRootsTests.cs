namespace Pure.DI.IntegrationTests;

public class LightweightRootsTests
{
    [Fact]
    public async Task ShouldSupportLightweightRoot()
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
                               }
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 1 {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().As(Lifetime.PerResolve).To<Dependency>()
                                           .Root<Service>("Root", kind: RootKinds.Light);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Root;
                                       var service2 = composition.Resolve<Service>();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 1 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportLightweightRootWhenNoName()
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
                               }
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 1 {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().As(Lifetime.PerResolve).To<Dependency>()
                                           .Root<Service>(kind: RootKinds.Light);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<Service>();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportSeveralLightweightRoots()
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
                               }
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize 1 {depName}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                               
                               class Xyz
                               {
                                    public Xyz()
                                    {
                                        Console.WriteLine("Xyz");
                                    }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => "Abc")
                                           .Bind().As(Lifetime.PerResolve).To<Dependency>()
                                           .Root<Service>("Root", kind: RootKinds.Light)
                                           .Root<Xyz>("Xyz", kind: RootKinds.Light);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service1 = composition.Root;
                                       var service2 = composition.Resolve<Service>();
                                       var xyz = composition.Xyz;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize 1 Abc", "Initialize 1 Abc", "Xyz"], result);
    }
}