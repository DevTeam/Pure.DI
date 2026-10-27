namespace Pure.DI.IntegrationTests;

/// <summary>
/// A binding with several tags contributes one element to a collection, resolved by a tag the binding still owns.
/// </summary>
public class CollectionMultipleTagsTests
{
    [Theory]
    [InlineData(""".Bind<IService>("a", "b").To<XService>().Bind<IService>(Tag.Unique).To<YService>()""", "X,Y")]
    [InlineData(""".Bind<IService>("a", "b").To<XService>().Bind<IService>("b").To<YService>()""", "X,Y")]
    [InlineData(""".Bind<IService>("b").To<YService>().Bind<IService>("a", "b").To<XService>()""", "X")]
    [InlineData(""".Bind<IService>("a", "b").To<XService>().Bind<IService>("a").To<YService>()""", "X,Y")]
    [InlineData(""".Bind<IService>("a", "b").To<XService>().Bind<IService>("a", "b").To<YService>()""", "Y")]
    public async Task ShouldAddOneElementPerBindingThatOwnsATag(string bindings, string expected)
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { string Name { get; } }

                           sealed class XService : IService { public string Name => "X"; }

                           sealed class YService : IService { public string Name => "Y"; }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   BINDINGS
                                   .Root<IEnumerable<IService>>("Services");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(string.Join(",", new Composition().Services.Select(i => i.Name).OrderBy(i => i)));
                           }
                           """.Replace("BINDINGS", bindings).RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe([expected], result);
    }
}
