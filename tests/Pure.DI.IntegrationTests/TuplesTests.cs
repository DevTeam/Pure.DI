namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to the support for tuples as roots or dependencies.
/// </summary>
public class TuplesTests
{

    [Fact]
    public async Task ShouldSupportTupleRoot()
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
                           
                               internal readonly record struct Point(int X, int Y);
                           
                               internal interface IService
                               {
                                   IDependency Dependency { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   private readonly Lazy<IDependency> _dependency;
                           
                                   public Service(Lazy<IDependency> dependency)
                                   {
                                       _dependency = dependency;
                                   }
                           
                                   public IDependency Dependency => _dependency.Value;
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<Point>().To(_ => new Point(7, 9))
                                           .Bind<IService>().To<Service>()
                                           .Root<(IService Service, Point Point, IDependency Dependency)>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var tuple = composition.Root;
                                       Console.WriteLine(tuple);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["(Sample.Service, Point { X = 7, Y = 9 }, Sample.Dependency)"], result);
    }
    [Fact]
    public async Task ShouldBindUnnamedTupleLiteralToNamedTupleContract()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Service
                               {
                                   public Service((string Street, string City) address) => Address = address;

                                   public (string Street, string City) Address { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<(string Street, string City)>().To(_ => ("Main St", "Springfield"))
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root.Address.Street);
                                       Console.WriteLine(root.Address.City);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Main St", "Springfield"], result);
    }

    [Fact]
    public async Task ShouldSupportTuples()
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
                           
                               internal readonly record struct Point(int X, int Y);
                           
                               internal interface IService
                               {
                                   IDependency Dependency { get; }
                               }
                           
                               internal class Service : IService
                               {
                                   private readonly IDependency _dependency;
                           
                                   public Service((Point Point, Lazy<IDependency> Dependency) dep)
                                   {
                                       _dependency = dep.Dependency.Value;
                                   }
                           
                                   public IDependency Dependency => _dependency;
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<Point>().To(_ => new Point(7, 9))
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
    }
}