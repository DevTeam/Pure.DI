// ReSharper disable StringLiteralTypo

namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
///     Reproduces the "Constructor selection needs an explicit factory" observation from
///     AI.Client: with several constructors the generator may pick one that cannot be resolved
///     even though another constructor of the same type is fully resolvable.
/// </summary>
public class CtorSelectionIssueTests
{
    [Fact]
    public async Task ShouldPreferResolvableConstructorOverUnresolvableOptionalCandidate()
    {
        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IProjectStorageLocation { }

                               sealed class ProjectStorageLocation : IProjectStorageLocation { }

                               sealed class JsonLineFileLoggerProvider
                               {
                                   public string Mode { get; }

                                   public JsonLineFileLoggerProvider(string rootDirectory)
                                       => Mode = "string:" + rootDirectory;

                                   public JsonLineFileLoggerProvider(IProjectStorageLocation location, int retentionDays = 14)
                                       => Mode = "location:" + retentionDays;
                               }

                               sealed class Service
                               {
                                   private readonly JsonLineFileLoggerProvider _provider;

                                   public Service(JsonLineFileLoggerProvider provider) => _provider = provider;

                                   public string Value => _provider.Mode;

                                   public override string ToString() => _provider.Mode;
                               }

                               partial class Composition
                               {
                                   void Setup() => DI.Setup()
                                       .Bind<IProjectStorageLocation>().To<ProjectStorageLocation>()
                                       .Bind<JsonLineFileLoggerProvider>().To<JsonLineFileLoggerProvider>()
                                       .Root<Service>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Errors.ShouldBeEmpty();
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["location:14"], result);
    }

    [Fact]
    public async Task ShouldPreferResolvableConstructorDeclaredAfterUnresolvableOne()
    {
        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency { }

                               sealed class Dependency : IDependency { }

                               sealed class Service
                               {
                                   public string Mode { get; }

                                   public Service(string value) => Mode = "string:" + value;

                                   public Service(IDependency dependency) => Mode = "dependency";
                               }

                               partial class Composition
                               {
                                   void Setup() => DI.Setup()
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<Service>().To<Service>()
                                       .Root<Service>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.Mode);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Errors.ShouldBeEmpty();
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["dependency"], result);
    }

    [Fact]
    public async Task ShouldPreferResolvableConstructorForAutoBinding()
    {
        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IProjectStorageLocation { }

                               sealed class ProjectStorageLocation : IProjectStorageLocation { }

                               sealed class JsonLineFileLoggerProvider
                               {
                                   public string Mode { get; }

                                   public JsonLineFileLoggerProvider(string rootDirectory)
                                       => Mode = "string:" + rootDirectory;

                                   public JsonLineFileLoggerProvider(IProjectStorageLocation location, int retentionDays = 14)
                                       => Mode = "location:" + retentionDays;
                               }

                               sealed class Service
                               {
                                   private readonly JsonLineFileLoggerProvider _provider;

                                   public Service(JsonLineFileLoggerProvider provider) => _provider = provider;

                                   public override string ToString() => _provider.Mode;
                               }

                               partial class Composition
                               {
                                   void Setup() => DI.Setup()
                                       .Bind<IProjectStorageLocation>().To<ProjectStorageLocation>()
                                       .Root<Service>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Errors.ShouldBeEmpty();
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["location:14"], result);
    }
}
