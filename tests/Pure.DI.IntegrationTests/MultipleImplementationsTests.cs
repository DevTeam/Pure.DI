namespace Pure.DI.IntegrationTests;

using Core;

public class MultipleImplementationsTests
{
    [Fact]
    public async Task ShouldWarnWhenMultipleImplementationsHaveNoTag()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { }
                           sealed class Service1 : IService { }
                           sealed class Service2 : IService { }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IService>().To<Service1, Service2>()
                                   .Root<IService>("Service");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.GetType().Name);
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["Service2"], result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding && i.Locations.FirstOrDefault().GetSource() == "Service2").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningBindingNotUsed && i.Locations.FirstOrDefault().GetSource() == "Service1").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldWarnWhenMultipleImplementationsShareAnOrdinaryTag()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { }
                           sealed class Service1 : IService { }
                           sealed class Service2 : IService { }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IService>("main").To<Service1, Service2>()
                                   .Root<IService>("Service", "main");
                           }

                           public class Program
                           {
                               public static void Main() => Console.WriteLine(new Composition().Service.GetType().Name);
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.StdOut.ShouldBe(["Service2"], result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding && i.Locations.FirstOrDefault().GetSource() == "Service2").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningBindingNotUsed && i.Locations.FirstOrDefault().GetSource() == "Service1").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldSupportSixtyFourImplementationTypes()
    {
        var implementations = string.Join(", ", Enumerable.Repeat("Service", 64));
        var source = """
                     using System;
                     using Pure.DI;

                     namespace Sample;

                     interface IService { }
                     sealed class Service : IService { }

                     partial class Composition
                     {
                         void Setup() => DI.Setup()
                             .Bind<IService>(Tag.Unique).To<IMPLEMENTATIONS>()
                             .Root<IService[]>("Services");
                     }

                     public class Program
                     {
                         public static void Main() => Console.WriteLine(new Composition().Services.Length);
                     }
                     """.Replace("IMPLEMENTATIONS", implementations);

        var result = await source.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["64"], result);
    }

    [Fact]
    public async Task ShouldBindMultipleImplementationsWithLifetime()
    {
        var result = await """
                           using System;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IService { }
                           sealed class Service1 : IService { }
                           sealed class Service2 : IService { }
                           sealed class Service3 : IService { }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IService>(Tag.Unique).As(Lifetime.Singleton)
                                   .To<Service1, Service2, Service3>()
                                   .Root<IService[]>("Services");
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   var services = composition.Services;
                                   Console.WriteLine(string.Join(",", services.Select(service => service.GetType().Name)));
                                   Console.WriteLine(services.SequenceEqual(composition.Services));
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Service1,Service2,Service3", "True"], result);
    }
}
