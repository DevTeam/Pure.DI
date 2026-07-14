#if ROSLYN5_6_OR_GREATER
namespace Pure.DI.IntegrationTests;

using Core;

public class UnionTypesTests
{
    private const string UnionRuntimePolyfill =
        """
        namespace System.Runtime.CompilerServices
        {
            public interface IUnion
            {
                object? Value { get; }
            }

            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
            public sealed class UnionAttribute : Attribute;
        }
        """;

    private static Options PreviewOptions => new(
        LanguageVersion.Preview,
        PreprocessorSymbols: ["NET", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER"]);

    [Fact]
    public async Task ShouldSupportUnionContractWithImplementationBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<PaymentGateway>().To<StripeGateway>()
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Gateway).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldSupportUnionContractWithSimpleFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<PaymentGateway>().To(() => new StripeGateway())
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Gateway).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldSupportUnionContractWithContextFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<PaymentGateway>().To<PaymentGateway>(ctx =>
                                           {
                                               ctx.Inject<StripeGateway>(out var stripe);
                                               return stripe;
                                           })
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Gateway).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldSupportUnionInjectionForAllInjectionSites()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               class Service
                               {
                                   private readonly PaymentGateway _fromCtor;

                                   public Service(PaymentGateway gateway)
                                   {
                                       _fromCtor = gateway;
                                   }

                                   [Ordinal(1)]
                                   public PaymentGateway FromField;

                                   [Ordinal(2)]
                                   public PaymentGateway FromProperty { get; set; }

                                   private PaymentGateway _fromMethod;

                                   [Ordinal(3)]
                                   public void Initialize(PaymentGateway gateway) =>
                                       _fromMethod = gateway;

                                   public void Print()
                                   {
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)_fromCtor).Value);
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)FromField).Value);
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)FromProperty).Value);
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)_fromMethod).Value);
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<PaymentGateway>().To<StripeGateway>()
                                           .Root<Service>("Service");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       new Composition().Service.Print();
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe", "Stripe", "Stripe", "Stripe"], result);
    }

    [Fact]
    public async Task ShouldResolveUnionFromSingleCaseBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>().To<StripeGateway>()
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Gateway).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldSupportTaggedUnionCaseSelection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>("stripe").To<StripeGateway>()
                                           .Bind<BankGateway>("bank").To<BankGateway>()
                                           .Root<PaymentGateway>("Stripe", "stripe")
                                           .Root<PaymentGateway>("Bank", "bank");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)composition.Stripe).Value);
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)composition.Bank).Value);
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe", "Bank"], result);
    }

    [Fact]
    public async Task ShouldPreferExactUnionBindingOverCaseCandidates()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<PaymentGateway>().To<StripeGateway>()
                                           .Bind<BankGateway>().To<BankGateway>()
                                           .Root<PaymentGateway>("Gateway")
                                           .Root<BankGateway>("Bank");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Gateway).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldSupportUnionResolveMethods()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       DI.Setup("Composition")
                                           .Bind<PaymentGateway>().To<StripeGateway>()
                                           .Root<PaymentGateway>();
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Resolve<PaymentGateway>()).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldNotHideSourceLifetimeBehindUnionConversion()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>().As(Lifetime.Singleton).To<StripeGateway>()
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var value1 = ((System.Runtime.CompilerServices.IUnion)composition.Gateway).Value;
                                       var value2 = ((System.Runtime.CompilerServices.IUnion)composition.Gateway).Value;
                                       Console.WriteLine(ReferenceEquals(value1, value2));
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSingletonLifetimeForExplicitUnionBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<PaymentGateway>().As(Lifetime.Singleton).To<StripeGateway>()
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var value1 = ((System.Runtime.CompilerServices.IUnion)composition.Gateway).Value;
                                       var value2 = ((System.Runtime.CompilerServices.IUnion)composition.Gateway).Value;
                                       Console.WriteLine(ReferenceEquals(value1, value2));
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldShowErrorWhenSeveralUnionCaseBindingsAreApplicable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>().To<StripeGateway>()
                                           .Bind<BankGateway>().To<BankGateway>()
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorAmbiguousUnionCaseBindings).ShouldBe(1, result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBe(0, result);
        var error = result.Errors.Single(i => i.Id == LogId.ErrorAmbiguousUnionCaseBindings);
        error.Message.ShouldContain("Sample.StripeGateway");
        error.Message.ShouldContain("Sample.BankGateway");
        error.Message.ShouldContain("Sample.PaymentGateway");
    }

    [Fact]
    public async Task ShouldShowErrorWhenSeveralUnionCaseBindingsShareTheSameTag()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>("pay").To<StripeGateway>()
                                           .Bind<BankGateway>("pay").To<BankGateway>()
                                           .Root<PaymentGateway>("Gateway", "pay");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorAmbiguousUnionCaseBindings).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldNotUseTaggedCaseBindingForUntaggedUnionInjection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>("stripe").To<StripeGateway>()
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBeGreaterThan(0, result);
        result.Errors.Count(i => i.Id == LogId.ErrorAmbiguousUnionCaseBindings).ShouldBe(0, result);
    }

    [Fact]
    public async Task ShouldShowErrorWhenNoUnionCaseBindingIsAvailable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBeGreaterThan(0, result);
    }

    [Fact]
    public async Task ShouldShowLifetimeDefectThroughUnionConversion()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class Terminal;

                               class StripeGateway
                               {
                                   public StripeGateway(Terminal terminal)
                                   {
                                   }

                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               class Consumer
                               {
                                   public Consumer(PaymentGateway gateway)
                                   {
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>().As(Lifetime.Scoped).To<StripeGateway>()
                                           .Bind<Consumer>().As(Lifetime.Singleton).To<Consumer>()
                                           .Root<Consumer>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeFalse(result);
        result.Logs.Count(i => i.Id == LogId.ErrorLifetimeDefect).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldNotTreatNonUnionImplicitConversionAsContract()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class Fahrenheit;

                               class Celsius
                               {
                                   public static implicit operator Fahrenheit(Celsius celsius) => new Fahrenheit();
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<Fahrenheit>().To<Celsius>()
                                           .Root<Fahrenheit>("Temperature");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorNotImplementedContract).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldSupportUserDefinedImplicitConversionThatShadowsUnionConversion()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public static implicit operator PaymentGateway(StripeGateway value) =>
                                       new BankGateway();
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<PaymentGateway>().To<StripeGateway>()
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Gateway).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Bank"], result);
    }

    [Fact]
    public async Task ShouldResolveClassUnionFromSingleCaseBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class BankGateway;

                               [System.Runtime.CompilerServices.Union]
                               class PaymentGateway : System.Runtime.CompilerServices.IUnion
                               {
                                   public PaymentGateway(StripeGateway value) => Value = value;

                                   public PaymentGateway(BankGateway value) => Value = value;

                                   public object? Value { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>().To<StripeGateway>()
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(new Composition().Gateway.Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldInferUnionWhenUserDefinedImplicitConversionShadowsUnionConversion()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway
                               {
                                   public static implicit operator PaymentGateway(StripeGateway value) =>
                                       new BankGateway();
                               }

                               class BankGateway
                               {
                                   public override string ToString() => "Bank";
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>().To<StripeGateway>()
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Gateway).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Bank"], result);
    }

    [Fact]
    public async Task ShouldNotTreatUserDefinedConversionFromNonCaseTypeAsUnionContract()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway;

                               class BankGateway;

                               class ExternalGateway
                               {
                                   public static implicit operator PaymentGateway(ExternalGateway value) =>
                                       new StripeGateway();
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<PaymentGateway>().To<ExternalGateway>()
                                           .Root<PaymentGateway>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorNotImplementedContract).ShouldBe(1, result);
    }
}
#endif
