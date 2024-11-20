namespace Pure.DI.IntegrationTests;

using Core;

public class TuplesTests
{
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

    [Fact]
    public async Task ShouldSupportTuplesWhenCannotResolve()
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
                           
                                   public Service((Point Point, IDependency Dependency) dep)
                                   {
                                       _dependency = dep.Dependency;
                                   }
                           
                                   public IDependency Dependency => _dependency;
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
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
        result.Success.ShouldBeFalse(result);
        var errors = result.Logs.Where(i => i.Id == LogId.ErrorUnableToResolve).ToList();
        errors.Count.ShouldBe(1, result);
    }

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
}