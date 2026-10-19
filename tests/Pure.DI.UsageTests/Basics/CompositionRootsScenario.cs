/*
$v=true
$t=QuickStart
$t=Basics
$p=2
$p=QuickStart:3
$d=Composition roots
$sa=Root binding
$sa=Roots
$sa=Composition root kinds
$h=A composition root is the explicit entry point into the object graph: `Root<T>(name, tag)` generates a property or a method that creates `T` together with all of its dependencies, so application code never asks the container for a type.
$h=Declare one root per real entry point - a window, a controller, a CLI command, a message handler - and keep everything else private to the composition, so the composition constructor and its roots are the whole public surface of the graph.
$h=Unlike `Resolve<T>()`, a root is an ordinary member: it cannot fail with a missing-root exception, and the compiler knows its exact type, which keeps nullable analysis and refactoring honest.
$h=In classic containers the graph is reached through `T Resolve<T>()` or `object GetService(Type type)`; here every root is generated at compile time, so entry points are explicit and discoverable in the generated code.
$h=>[!TIP]
$h=>There is no hard limit on roots, but prefer a small number. Ideally, an application has a single composition root.
$f=The root name becomes a member name, so it follows C# member naming rules; an invalid name is reported as `DIE004` and a duplicate one as `DIE005`.
$f=A root declared without a name is anonymous: it is reachable only from `Resolve` methods, which suits diagnostics and tests rather than production call sites.
$f=A root that consumes at least one root argument becomes a method, and only the arguments its own object graph uses appear in the signature.
$f=`// Resolve = Off` before `Setup` drops the anonymous roots and the untyped API, so keep every entry point as a named root; this also shortens generation.
$f=Prefer `RootBind<T>(name).To<TImpl>()` when a type is both a binding and a root, and `Roots<T>` when the implementations come from the assembly, as in plugin scenarios.
$f=Limitations: too many public roots increase composition API surface and make architecture boundaries harder to track; exposing internal services as roots is the usual way this happens.
$f=See also: [Resolve methods](resolve-methods.md), [Root arguments](root-arguments.md).
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.CompositionRootsScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }
public class Scenario
{
    [Fact]
    public void Run()
    {
        // {
        DI.Setup(nameof(Composition))
            .Bind<IInvoiceGenerator>().To<PdfInvoiceGenerator>()
            .Bind<IInvoiceGenerator>("Online").To<HtmlInvoiceGenerator>()
            .Bind<ILogger>().To<FileLogger>()

            // Specifies to create a regular composition root
            // of type "IInvoiceGenerator" with the name "InvoiceGenerator".
            // This will be the main entry point for invoice generation.
            .Root<IInvoiceGenerator>("InvoiceGenerator")

            // Specifies to create an anonymous composition root
            // that is only accessible from "Resolve()" methods.
            // This is useful for auxiliary types or testing.
            .Root<ILogger>()

            // Specifies to create a regular composition root
            // of type "IInvoiceGenerator" with the name "OnlineInvoiceGenerator"
            // using the "Online" tag to differentiate implementations.
            .Root<IInvoiceGenerator>("OnlineInvoiceGenerator", "Online");

        var composition = new Composition();

        // Resolves the default invoice generator (PDF) with all its dependencies
        // invoiceGenerator = new PdfInvoiceGenerator(new FileLogger());
        var invoiceGenerator = composition.InvoiceGenerator;

        // Resolves the online invoice generator (HTML)
        // onlineInvoiceGenerator = new HtmlInvoiceGenerator();
        var onlineInvoiceGenerator = composition.OnlineInvoiceGenerator;

        // All and only the roots of the composition
        // can be obtained by Resolve method.
        // Here we resolve the private root 'ILogger'.
        var logger = composition.Resolve<ILogger>();

        // We can also resolve tagged roots dynamically if needed
        var tagged = composition.Resolve<IInvoiceGenerator>("Online");
        // }
        invoiceGenerator.ShouldBeOfType<PdfInvoiceGenerator>();
        tagged.ShouldBeOfType<HtmlInvoiceGenerator>();
        composition.SaveClassDiagram();
    }
}

// {
// Common logger interface used across the system
interface ILogger;

// Concrete implementation of a logger that writes to a file
class FileLogger : ILogger;

// Abstract definition of an invoice generator
interface IInvoiceGenerator;

// Implementation for generating PDF invoices, dependent on ILogger
class PdfInvoiceGenerator(ILogger logger) : IInvoiceGenerator;

// Implementation for generating HTML invoices for online viewing
class HtmlInvoiceGenerator : IInvoiceGenerator;
// }
