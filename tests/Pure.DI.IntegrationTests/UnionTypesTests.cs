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
        error.Message.ShouldContain("To<StripeGateway>()");
        error.Message.ShouldContain("To<BankGateway>()");
        error.Locations.Length.ShouldBeGreaterThanOrEqualTo(3);
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

    [Fact]
    public async Task ShouldResolveUnionFromCompositionArgument()
    {
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

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Arg<StripeGateway>("gateway", "primary")
                                           .Root<PaymentGateway>("Gateway", "primary");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition(new StripeGateway()).Gateway).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldResolveUnionFromRootArgument()
    {
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

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .RootArg<StripeGateway>("gateway", "primary")
                                           .Root<PaymentGateway>("Gateway", "primary");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Gateway(new StripeGateway())).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldResolveClosedGenericUnionFromSingleCaseBinding()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               record Success<T>(T Value);

                               record Failure(string Error);

                               union Result<T>(Success<T>, Failure);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<int>().To(() => 42)
                                           .Bind<Success<int>>().To<Success<int>>()
                                           .Root<Result<int>>("Result");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((Success<int>)((System.Runtime.CompilerServices.IUnion)new Composition().Result).Value!).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["42"], result);
    }

    [Fact]
    public async Task ShouldResolveUnionThroughStandardConversionToInterfaceCase()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               interface IPaymentGateway;

                               class StripeGateway : IPaymentGateway
                               {
                                   public override string ToString() => "Stripe";
                               }

                               class FallbackGateway;

                               union PaymentGateway(IPaymentGateway, FallbackGateway);

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

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldResolveNullableUnionFromSingleCaseBinding()
    {
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

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>().To<StripeGateway>()
                                           .Root<PaymentGateway?>("Gateway");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Gateway!.Value).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldNotChainUnionConversions()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class Visa;

                               class MasterCard;

                               class Cash;

                               union CardPayment(Visa, MasterCard);

                               union Payment(CardPayment, Cash);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<Visa>().To<Visa>()
                                           .Root<Payment>("Payment");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve).ShouldBeGreaterThan(0, result);
    }

    [Fact]
    public async Task ShouldCreateUnionOnDemand()
    {
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

                               union PaymentGateway(StripeGateway, BankGateway);

                               class Checkout(Func<PaymentGateway> gatewayFactory)
                               {
                                   public PaymentGateway Gateway => gatewayFactory();
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>().To<StripeGateway>()
                                           .Root<Checkout>("Checkout");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Checkout.Gateway).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericUnionRoot()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class Success<T>
                               {
                                   public override string ToString() => typeof(T).Name;
                               }

                               class Failure;

                               union Result<T>(Success<T>, Failure);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<Success<TT>>().To<Success<TT>>()
                                           .Root<Result<TT>>("GetResult");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().GetResult<int>()).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Int32"], result);
    }

    [Fact]
    public async Task ShouldCollectTaggedUnionCases()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;
                           using static Pure.DI.Tag;

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
                                           .Bind<StripeGateway>(Unique).To<StripeGateway>()
                                           .Bind<BankGateway>(Unique).To<BankGateway>()
                                           .Root<IEnumerable<PaymentGateway>>("Gateways");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(string.Join(",", new Composition().Gateways.Select(i => ((System.Runtime.CompilerServices.IUnion)i).Value)));
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.Single().ShouldBeOneOf("Stripe,Bank", "Bank,Stripe");
    }

    [Theory]
    [InlineData("GatewayBase")]
    [InlineData("object")]
    public async Task ShouldResolveUnionThroughStandardConversionToCase(string caseType)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;

                             //UNION_POLYFILL//

                             namespace Sample
                             {
                                 abstract class GatewayBase;

                                 class StripeGateway : GatewayBase
                                 {
                                     public override string ToString() => "Stripe";
                                 }

                                 class BankGateway;

                                 union PaymentGateway({{caseType}}, BankGateway);

                                 static class Setup
                                 {
                                     private static void SetupComposition() =>
                                         // Resolve = Off
                                         DI.Setup("Composition")
                                             .Bind<StripeGateway>("case").To<StripeGateway>()
                                             .Root<PaymentGateway>("Gateway", "case");
                                 }

                                 public class Program
                                 {
                                     public static void Main() =>
                                         Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Gateway).Value);
                                 }
                             }
                             """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldResolveNullableCaseFromFactoryReturningNull()
    {
        var result = await """
                           #nullable enable
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               union LookupResult(string?, int);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<string?>().To(() => (string?)null)
                                           .Root<LookupResult>("Result");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Result).Value is null);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldResolveOuterUnionFromExplicitInnerUnionBinding()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class Visa
                               {
                                   public override string ToString() => "Visa";
                               }

                               class MasterCard;

                               class Cash;

                               union CardPayment(Visa, MasterCard);

                               union Payment(CardPayment, Cash);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<CardPayment>().To<Visa>()
                                           .Root<Payment>("Payment");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var inner = (System.Runtime.CompilerServices.IUnion)new Composition().Payment.Value!;
                                       Console.WriteLine(inner.Value);
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Visa"], result);
    }

    [Fact]
    public async Task ShouldResolveGenericUnionFromGenericFactory()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               record Success<T>(T Value);

                               record Failure(string Error);

                               union Result<T>(Success<T>, Failure);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<TT>().To(_ => default(TT)!)
                                           .Bind<Success<TT>>().To((TT value) => new Success<TT>(value))
                                           .Root<Result<TT>>("GetResult");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().GetResult<int>()).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Success { Value = 0 }"], result);
    }

    [Fact]
    public async Task ShouldShowAmbiguityForGenericUnionCases()
    {
        var result = await """
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class Success<T>;

                               class Failure;

                               union Result<T>(Success<T>, Failure);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       DI.Setup("Composition")
                                           .Bind<Success<TT>>().To<Success<TT>>()
                                           .Bind<Failure>().To<Failure>()
                                           .Root<Result<int>>("Result");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorAmbiguousUnionCaseBindings).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldCreateUnionOnDemandWithArgument()
    {
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

                               union PaymentGateway(StripeGateway, BankGateway);

                               class Checkout(Func<StripeGateway, PaymentGateway> gatewayFactory)
                               {
                                   public PaymentGateway Gateway => gatewayFactory(new StripeGateway());
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Root<Checkout>("Checkout");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Checkout.Gateway).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Theory]
    [InlineData("PaymentGateway[]", "composition.Gateways.Length")]
    [InlineData("ReadOnlySpan<PaymentGateway>", "composition.Gateways.Length")]
    public async Task ShouldCollectUnionCasesInEagerBclCollection(string collectionType, string countExpression)
    {
        var result = await $$"""
                             using System;
                             using Pure.DI;
                             using static Pure.DI.Tag;

                             //UNION_POLYFILL//

                             namespace Sample
                             {
                                 class StripeGateway;

                                 class BankGateway;

                                 union PaymentGateway(StripeGateway, BankGateway);

                                 static class Setup
                                 {
                                     private static void SetupComposition() =>
                                         // Resolve = Off
                                         DI.Setup("Composition")
                                             .Bind<StripeGateway>(Unique).To<StripeGateway>()
                                             .Bind<BankGateway>(Unique).To<BankGateway>()
                                             .Root<{{collectionType}}>("Gateways");
                                 }

                                 public class Program
                                 {
                                     public static void Main()
                                     {
                                         var composition = new Composition();
                                         Console.WriteLine({{countExpression}});
                                     }
                                 }
                             }
                             """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldCollectUnionCasesInAsyncEnumerable()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway;

                               class BankGateway;

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>(Unique).To<StripeGateway>()
                                           .Bind<BankGateway>(Unique).To<BankGateway>()
                                           .Root<IAsyncEnumerable<PaymentGateway>>("Gateways");
                               }

                               public class Program
                               {
                                   public static async Task Main()
                                   {
                                       var count = 0;
                                       await foreach (var _ in new Composition().Gateways)
                                       {
                                           count++;
                                       }

                                       Console.WriteLine(count);
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldResolveUnionThroughBoxingConversionToCase()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               union ValueResult(object, string);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<int>("value").To(() => 42)
                                           .Root<ValueResult>("Value", "value");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Value).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["42"], result);
    }

    [Fact]
    public async Task ShouldCreateUnionWithCustomDelegateArgument()
    {
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

                               union PaymentGateway(StripeGateway, BankGateway);

                               delegate PaymentGateway GatewayFactory(StripeGateway gateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<GatewayFactory>().To(ctx => new GatewayFactory(gateway =>
                                           {
                                               ctx.Override(gateway);
                                               ctx.Inject(out PaymentGateway result);
                                               return result;
                                           }))
                                           .Root<GatewayFactory>("Factory");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Factory(new StripeGateway())).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Stripe"], result);
    }

    [Fact]
    public async Task ShouldSupportConstraintsInGenericUnionBinding()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class Success<T>
                                   where T : class
                               {
                                   public override string ToString() => typeof(T).Name;
                               }

                               class Failure;

                               class TTRef;

                               union Result<T>(Success<T>, Failure)
                                   where T : class;

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .GenericTypeArgument<TTRef>()
                                           .Bind<Success<TTRef>>().To<Success<TTRef>>()
                                           .Root<Result<string>>("Result");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((System.Runtime.CompilerServices.IUnion)new Composition().Result).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["String"], result);
    }

    [Fact]
    public async Task ShouldDisposeSingletonUnionCaseOnce()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway : IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose Stripe");
                               }

                               class BankGateway;

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
                                       var first = ((System.Runtime.CompilerServices.IUnion)composition.Gateway).Value;
                                       var second = ((System.Runtime.CompilerServices.IUnion)composition.Gateway).Value;
                                       Console.WriteLine(ReferenceEquals(first, second));
                                       composition.Dispose();
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "Dispose Stripe"], result);
    }

    [Fact]
    public async Task ShouldPreservePerResolveLifetimeThroughUnionConversion()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway;

                               class BankGateway;

                               union PaymentGateway(StripeGateway, BankGateway);

                               class Checkout(PaymentGateway first, PaymentGateway second)
                               {
                                   public object? First => ((System.Runtime.CompilerServices.IUnion)first).Value;

                                   public object? Second => ((System.Runtime.CompilerServices.IUnion)second).Value;
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>().As(Lifetime.PerResolve).To<StripeGateway>()
                                           .Root<Checkout>("Checkout");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var first = composition.Checkout;
                                       var second = composition.Checkout;
                                       Console.WriteLine(ReferenceEquals(first.First, first.Second));
                                       Console.WriteLine(ReferenceEquals(first.First, second.First));
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False"], result);
    }

    [Fact]
    public async Task ShouldTrackAsyncDisposableUnionCaseCreatedByDelegate()
    {
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway : IAsyncDisposable
                               {
                                   public ValueTask DisposeAsync()
                                   {
                                       Console.WriteLine("DisposeAsync Stripe");
                                       return ValueTask.CompletedTask;
                                   }
                               }

                               class BankGateway;

                               union PaymentGateway(StripeGateway, BankGateway);

                               delegate PaymentGateway GatewayFactory();

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<StripeGateway>().As(Lifetime.Singleton).To<StripeGateway>()
                                           .Bind<GatewayFactory>().To(ctx => new GatewayFactory(() =>
                                           {
                                               ctx.Inject(out PaymentGateway gateway);
                                               return gateway;
                                           }))
                                           .Root<GatewayFactory>("Factory");
                               }

                               public class Program
                               {
                                   public static async Task Main()
                                   {
                                       var composition = new Composition();
                                       _ = composition.Factory();
                                       await composition.DisposeAsync();
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["DisposeAsync Stripe"], result);
    }

    [Fact]
    public async Task ShouldNotDisposeExternalUnionCaseDelegateArgument()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               class StripeGateway : IDisposable
                               {
                                   public int DisposeCount { get; private set; }

                                   public void Dispose() => DisposeCount++;
                               }

                               class BankGateway;

                               class OwnedResource : IDisposable
                               {
                                   public void Dispose()
                                   {
                                   }
                               }

                               union PaymentGateway(StripeGateway, BankGateway);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<OwnedResource>().As(Lifetime.Singleton).To<OwnedResource>()
                                           .Root<OwnedResource>("Owned")
                                           .Root<Func<StripeGateway, PaymentGateway>>("Factory");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var external = new StripeGateway();
                                       var composition = new Composition();
                                       _ = composition.Owned;
                                       _ = composition.Factory(external);
                                       composition.Dispose();
                                       Console.WriteLine(external.DisposeCount);
                                       external.Dispose();
                                       Console.WriteLine(external.DisposeCount);
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["0", "1"], result);
    }

    [Fact(Skip = "Roslyn 5.6 does not classify IUnionMembers provider conversions yet.")]
    public async Task ShouldResolveGenericCustomUnionWithMemberProvider()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               [System.Runtime.CompilerServices.Union]
                               sealed class Result<T> : Result<T>.IUnionMembers
                               {
                                   private object? _value;

                                   public interface IUnionMembers
                                   {
                                       public static Result<T> Create(T value) => new() { _value = value };

                                       public static Result<T> Create(Exception value) => new() { _value = value };

                                       public object? Value { get; }
                                   }

                                   object? IUnionMembers.Value => _value;
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<string>().To(() => "Success")
                                           .Root<Result<string>>("Result");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((Result<string>.IUnionMembers)new Composition().Result).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Success"], result);
    }

    [Fact(Skip = "Roslyn 5.6 does not classify IUnionMembers provider conversions yet.")]
    public async Task ShouldResolveCustomUnionThroughInMemberProviderFactory()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               readonly record struct CacheHit(int Value);

                               [System.Runtime.CompilerServices.Union]
                               readonly struct CacheResult : CacheResult.IUnionMembers
                               {
                                   private readonly object? _value;

                                   private CacheResult(object value) => _value = value;

                                   public interface IUnionMembers
                                   {
                                       public static CacheResult Create(in CacheHit value) => new(value);

                                       public static CacheResult Create(Exception value) => new(value);

                                       public object? Value { get; }
                                   }

                                   object? IUnionMembers.Value => _value;
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<CacheHit>().To(() => new CacheHit(42))
                                           .Root<CacheResult>("Result");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(((CacheResult.IUnionMembers)new Composition().Result).Value);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["CacheHit { Value = 42 }"], result);
    }

    [Fact(Skip = "Roslyn 5.6 does not classify IUnionMembers provider conversions yet.")]
    public async Task ShouldShowAmbiguityForCustomUnionMemberProviderCases()
    {
        var result = await """
                           using System;
                           using Pure.DI;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               [System.Runtime.CompilerServices.Union]
                               sealed class Result<T> : Result<T>.IUnionMembers
                               {
                                   private object? _value;

                                   public interface IUnionMembers
                                   {
                                       public static Result<T> Create(T value) => new() { _value = value };

                                       public static Result<T> Create(Exception value) => new() { _value = value };

                                       public object? Value { get; }
                                   }

                                   object? IUnionMembers.Value => _value;
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       DI.Setup("Composition")
                                           .Bind<string>().To(() => "Success")
                                           .Bind<Exception>().To(() => new Exception("Failure"))
                                           .Root<Result<string>>("Result");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorAmbiguousUnionCaseBindings).ShouldBe(1, result);
    }

    [Theory]
    [InlineData("List<PaymentGateway>")]
    [InlineData("IReadOnlyList<PaymentGateway>")]
    [InlineData("ReadOnlyCollection<PaymentGateway>")]
    [InlineData("ImmutableArray<PaymentGateway>")]
    [InlineData("IImmutableList<PaymentGateway>")]
    [InlineData("HashSet<PaymentGateway>")]
    public async Task ShouldCollectUnionCasesInBclCollection(string collectionType)
    {
        var result = await $$"""
                             using System;
                             using System.Collections.Generic;
                             using System.Collections.Immutable;
                             using System.Collections.ObjectModel;
                             using System.Linq;
                             using Pure.DI;
                             using static Pure.DI.Tag;

                             //UNION_POLYFILL//

                             namespace Sample
                             {
                                 class StripeGateway;

                                 class BankGateway;

                                 union PaymentGateway(StripeGateway, BankGateway);

                                 static class Setup
                                 {
                                     private static void SetupComposition() =>
                                         // Resolve = Off
                                         DI.Setup("Composition")
                                             .Bind<StripeGateway>(Unique).To<StripeGateway>()
                                             .Bind<BankGateway>(Unique).To<BankGateway>()
                                             .Root<{{collectionType}}>("Gateways");
                                 }

                                 public class Program
                                 {
                                     public static void Main() =>
                                         Console.WriteLine(new Composition().Gateways.Count());
                                 }
                             }
                             """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldCollectGenericUnionCasesInImmutableCollection()
    {
        var result = await """
                           using System;
                           using System.Collections.Immutable;
                           using Pure.DI;
                           using static Pure.DI.Tag;

                           //UNION_POLYFILL//

                           namespace Sample
                           {
                               record Success<T>(T Value);

                               record Failure(string Error);

                               union Result<T>(Success<T>, Failure);

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                       DI.Setup("Composition")
                                           .Bind<Success<int>>(Unique).To(() => new Success<int>(42))
                                           .Bind<Failure>(Unique).To(() => new Failure("Unavailable"))
                                           .Root<ImmutableArray<Result<int>>>("Results");
                               }

                               public class Program
                               {
                                   public static void Main() =>
                                       Console.WriteLine(new Composition().Results.Length);
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    [Fact]
    public async Task ShouldResolveTaggedUnionCollectionInjection()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
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

                               class GatewaySet([Tag("primary")] IEnumerable<PaymentGateway> gateways)
                               {
                                   public IReadOnlyList<PaymentGateway> Gateways { get; } = gateways.ToList();
                               }

                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       // Resolve = Off
                                           DI.Setup("Composition")
                                               .Bind<StripeGateway>("primary").To<StripeGateway>()
                                               .Bind<BankGateway>("secondary").To<BankGateway>()
                                           .Root<GatewaySet>("GatewaySet");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var gateways = new Composition().GatewaySet.Gateways;
                                       Console.WriteLine(gateways.Count);
                                       Console.WriteLine(string.Join(",", gateways
                                           .Select(i => ((System.Runtime.CompilerServices.IUnion)i).Value!.ToString())
                                           .OrderBy(i => i)));
                                   }
                               }
                           }
                           """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2", "Bank,Stripe"], result);
    }

    [Fact]
    public void ShouldKeepMemberProviderSupportCompilerDriven()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using System;

            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
                public sealed class UnionAttribute : Attribute;
            }

            namespace Sample
            {
                [System.Runtime.CompilerServices.Union]
                sealed class Result<T> : Result<T>.IUnionMembers
                {
                    private object? _value;

                    public interface IUnionMembers
                    {
                        public static Result<T> Create(T value) => new() { _value = value };

                        public static Result<T> Create(Exception value) => new() { _value = value };

                        public object? Value { get; }
                    }

                    object? IUnionMembers.Value => _value;
                }
            }
            """,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create(
            "Sample",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var sourceType = compilation.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_String);
        var targetType = compilation.GetTypeByMetadataName("Sample.Result`1")!.Construct(sourceType);

#pragma warning disable RSEXPERIMENTAL006
        var conversion = compilation.ClassifyConversion(sourceType, targetType);
        conversion.IsUnion.ShouldBeFalse();
#pragma warning restore RSEXPERIMENTAL006
    }

    [Theory]
    [InlineData("List<PaymentGateway>")]
    [InlineData("IEnumerable<PaymentGateway>")]
    public async Task ShouldDisposeSingletonUnionCaseResolvedThroughCollection(string collectionType)
    {
        var result = await $$"""
                             using System;
                             using System.Collections.Generic;
                             using System.Linq;
                             using Pure.DI;

                             //UNION_POLYFILL//

                             namespace Sample
                             {
                                 class StripeGateway : IDisposable
                                 {
                                     public void Dispose() => Console.WriteLine("Dispose Stripe");
                                 }

                                 class BankGateway;

                                 union PaymentGateway(StripeGateway, BankGateway);

                                 static class Setup
                                 {
                                     private static void SetupComposition() =>
                                         // Resolve = Off
                                         DI.Setup("Composition")
                                             .Bind<StripeGateway>().As(Lifetime.Singleton).To<StripeGateway>()
                                             .Root<{{collectionType}}>("Gateways");
                                 }

                                 public class Program
                                 {
                                     public static void Main()
                                     {
                                         var composition = new Composition();
                                         _ = composition.Gateways.Count();
                                         composition.Dispose();
                                     }
                                 }
                             }
                             """.Replace("//UNION_POLYFILL//", UnionRuntimePolyfill).RunAsync(PreviewOptions);

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Dispose Stripe"], result);
    }

}
#endif
