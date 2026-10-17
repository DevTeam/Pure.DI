namespace Pure.DI.IntegrationTests;

using Core;

public class SimplifiedBindingContractInferenceTests
{
    public interface IMetadataBaseContract { }
    public interface IMetadataDerivedContract : IMetadataBaseContract { }
    public sealed class MetadataRouter : IMetadataBaseContract { }
    public sealed class MetadataRepository : IMetadataDerivedContract { }

    [Fact]
    public async Task ShouldInferOnlyDirectContractsForTypesFromReferencedAssembly()
    {
        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using IBaseContract = Pure.DI.IntegrationTests.SimplifiedBindingContractInferenceTests.IMetadataBaseContract;
                           using Router = Pure.DI.IntegrationTests.SimplifiedBindingContractInferenceTests.MetadataRouter;
                           using Repository = Pure.DI.IntegrationTests.SimplifiedBindingContractInferenceTests.MetadataRepository;

                           namespace Sample;

                           static class Setup
                           {
                               public static void Configure() => DI.Setup("BaseComposition", CompositionKind.Internal)
                                   .Singleton<Router, Repository>();
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .DependsOn("BaseComposition")
                                   .Root<IBaseContract>("Root");
                           }

                           public class Program
                           {
                               public static void Main() =>
                                   Console.WriteLine(new Composition().Root.GetType().Name);
                           }
                           """
            .RunAsync(new Options(
                LanguageVersion.Preview,
                MetadataReferences: [MetadataReference.CreateFromFile(typeof(SimplifiedBindingContractInferenceTests).Assembly.Location)]));

        // Then
        result.Errors.ShouldBeEmpty(result);
        result.Warnings.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["MetadataRouter"], result);
    }

    public static TheoryData<string> Bindings => new()
    {
        {
            """
            .Bind().To<FirstService>()
            .Bind().To<SecondService>()
            .Bind().To<FirstConcreteService>()
            .Bind().To<SecondConcreteService>()
            """
        },
        {
            """
            .PerBlock<FirstService>()
            .PerBlock<SecondService>()
            .PerBlock<FirstConcreteService>()
            .PerBlock<SecondConcreteService>()
            """
        },
        {
            """
            .PerBlock<FirstService, SecondService, FirstConcreteService, SecondConcreteService>()
            """
        },
        {
            """
            .PerBlock<Dependency, FirstService>(dependency => new FirstService(dependency))
            .PerBlock<Dependency, SecondService>(dependency => new SecondService(dependency))
            .PerBlock<Dependency, FirstConcreteService>(dependency => new FirstConcreteService(dependency))
            .PerBlock<Dependency, SecondConcreteService>(dependency => new SecondConcreteService(dependency))
            """
        }
    };

    [Theory]
    [MemberData(nameof(Bindings))]
    public async Task ShouldInferTheSameContractsForAllSimplifiedBindingForms(string bindings)
    {
        // When
        var result = await """
                           using System;
                           using System.Collections;
                           using System.Collections.Generic;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample;

                           interface IInheritedFromDirectInterface { }
                           interface IDirectAbstractContract : IInheritedFromDirectInterface { }
                           interface IDirectConcreteContract { }
                           interface ICustomSpecialContract { }
                           interface IInheritedFromAbstractBase { }
                           interface IInheritedFromConcreteBase { }

                           abstract class AbstractBase : IInheritedFromAbstractBase { }
                           class ConcreteBase : IInheritedFromConcreteBase { }
                           sealed class Dependency { }

                           sealed class FirstService :
                               AbstractBase,
                               IDirectAbstractContract,
                               ICustomSpecialContract,
                               IDisposable,
                               IAsyncDisposable,
                               IEnumerable<int>
                           {
                               public FirstService(Dependency dependency) { }
                               public void Dispose() { }
                               public ValueTask DisposeAsync() => ValueTask.CompletedTask;
                               public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)Array.Empty<int>()).GetEnumerator();
                               IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                           }

                           sealed class SecondService :
                               AbstractBase,
                               IDirectAbstractContract,
                               ICustomSpecialContract,
                               IDisposable,
                               IAsyncDisposable,
                               IEnumerable<int>
                           {
                               public SecondService(Dependency dependency) { }
                               public void Dispose() { }
                               public ValueTask DisposeAsync() => ValueTask.CompletedTask;
                               public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)Array.Empty<int>()).GetEnumerator();
                               IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                           }

                           sealed class FirstConcreteService : ConcreteBase, IDirectConcreteContract
                           {
                               public FirstConcreteService(Dependency dependency) { }
                           }

                           sealed class SecondConcreteService : ConcreteBase, IDirectConcreteContract
                           {
                               public SecondConcreteService(Dependency dependency) { }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .SpecialType<ICustomSpecialContract>()
                                   .Bind().To<Dependency>()
                           #bindings#
                                   .Root<FirstService>("First")
                                   .Root<SecondService>("Second")
                                   .Root<FirstConcreteService>("FirstConcrete")
                                   .Root<SecondConcreteService>("SecondConcrete");
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine($"{composition.First.GetType().Name}," +
                                                     $"{composition.Second.GetType().Name}," +
                                                     $"{composition.FirstConcrete.GetType().Name}," +
                                                     composition.SecondConcrete.GetType().Name);
                               }
                           }
                           """
            .Replace("#bindings#", bindings)
            .RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["FirstService,SecondService,FirstConcreteService,SecondConcreteService"], result);

        var overriddenContracts = result.Warnings
            .Where(i => i.Id == LogId.WarningOverriddenBinding)
            .Select(i => i.Message)
            .ToArray();

        overriddenContracts.Length.ShouldBe(3, result);
        overriddenContracts.Count(i => i.Contains("AbstractBase", StringComparison.Ordinal)).ShouldBe(1, result);
        overriddenContracts.Count(i => i.Contains("IDirectAbstractContract", StringComparison.Ordinal)).ShouldBe(1, result);
        overriddenContracts.Count(i => i.Contains("IDirectConcreteContract", StringComparison.Ordinal)).ShouldBe(1, result);

        foreach (var message in overriddenContracts)
        {
            message.ShouldNotContain("ConcreteBase");
            message.ShouldNotContain("IInheritedFromAbstractBase");
            message.ShouldNotContain("IInheritedFromConcreteBase");
            message.ShouldNotContain("IInheritedFromDirectInterface");
            message.ShouldNotContain("ICustomSpecialContract");
            message.ShouldNotContain("IDisposable");
            message.ShouldNotContain("IAsyncDisposable");
            message.ShouldNotContain("IEnumerable");
        }
    }
}
