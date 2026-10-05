// ReSharper disable StringLiteralTypo

namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
///     Characterizes the remaining AI.Client observations that are documented as expected
///     configuration limitations rather than generator defects (see the project instructions on
///     simplified binding): a parameterless <c>Bind()</c> infers only the implementation itself and
///     the abstract types it directly implements. Therefore a composite service exposed by its
///     inferred contract can be overridden, an interface inherited through an abstract base class is
///     not inferred, and the shared abstract base class is inferred by every implementation. A named
///     root also still cannot duplicate an anonymous root of the same contract.
/// </summary>
public class SimplifiedBindingLimitationsTests
{
    [Fact]
    public async Task ShouldWarnWhenCompositeContractIsInferredTwice()
    {
        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IMasterKeyStore { string Name { get; } }

                               sealed class FileMasterKeyStore : IMasterKeyStore
                               {
                                   public string Name => "file";
                               }

                               sealed class CompositeMasterKeyStore : IMasterKeyStore
                               {
                                   private readonly FileMasterKeyStore _file;

                                   public CompositeMasterKeyStore(FileMasterKeyStore file) => _file = file;

                                   public string Name => "composite:" + _file.Name;
                               }

                               sealed class Service
                               {
                                   private readonly IMasterKeyStore _store;

                                   public Service(IMasterKeyStore store) => _store = store;

                                   public override string ToString() => _store.Name;
                               }

                               partial class Composition
                               {
                                   void Setup() => DI.Setup()
                                       .Bind().To<FileMasterKeyStore>()
                                       .Bind().To<CompositeMasterKeyStore>()
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
        // Both simplified bindings expose IMasterKeyStore, so the contract is inferred twice and
        // the generator reports the override. Compilation still succeeds and the last binding wins.
        result.Errors.ShouldBeEmpty();
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding).ShouldBe(1, result);
        result.Warnings.Single(i => i.Id == LogId.WarningOverriddenBinding)
            .Message.Contains("IMasterKeyStore", StringComparison.Ordinal).ShouldBeTrue(result);
        result.StdOut.ShouldBe(["composite:file"], result);
    }

    [Fact]
    public async Task ShouldNotInferInterfaceInheritedThroughAbstractBaseClass()
    {
        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IToolPresentationAdapter { string Name { get; } }

                               abstract class BuiltInToolPresentationAdapter : IToolPresentationAdapter
                               {
                                   public abstract string Name { get; }
                               }

                               sealed class ReadToolPresentationAdapter : BuiltInToolPresentationAdapter
                               {
                                   public override string Name => "read";
                               }

                               sealed class WriteToolPresentationAdapter : BuiltInToolPresentationAdapter
                               {
                                   public override string Name => "write";
                               }

                               sealed class Service
                               {
                                   private readonly IReadOnlyCollection<IToolPresentationAdapter> _adapters;

                                   public Service(IReadOnlyCollection<IToolPresentationAdapter> adapters) =>
                                       _adapters = adapters;

                                   public override string ToString() =>
                                       string.Join(",", _adapters.Select(i => i.Name));
                               }

                               partial class Composition
                               {
                                   void Setup() => DI.Setup()
                                       .Bind().To<ReadToolPresentationAdapter>()
                                       .Bind().To<WriteToolPresentationAdapter>()
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
        // IToolPresentationAdapter is implemented through the base class, so "directly implements"
        // inference does not add it and the requested collection stays empty without any diagnostic
        // that points at the intended collection. The abstract base is directly implemented by both
        // adapters, so it is inferred by each of them and one binding overrides the other.
        result.Errors.ShouldBeEmpty();
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding).ShouldBe(1, result);
        result.Warnings.Single(i => i.Id == LogId.WarningOverriddenBinding)
            .Message.Contains("BuiltInToolPresentationAdapter", StringComparison.Ordinal).ShouldBeTrue(result);
        result.Warnings.Count(i => i.Id == LogId.WarningBindingNotUsed).ShouldBe(2, result);
        result.StdOut.ShouldBeEmpty(result);
    }

    [Fact]
    public async Task ShouldRejectNamedRootDuplicatingAnonymousRootOfSameContract()
    {
        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IChatService { }

                               sealed class ChatService : IChatService { }

                               partial class Composition
                               {
                                   void Setup() => DI.Setup()
                                       .Bind<IChatService>().To<ChatService>()
                                       .Root<IChatService>()
                                       .Root<IChatService>("Chats");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Chats);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeFalse();
        result.Errors.Count(i => i.Id == LogId.ErrorDuplicateRootName
                                 && i.Message.Contains("\"Chats\"", StringComparison.Ordinal)).ShouldBe(1, result);
    }
}
