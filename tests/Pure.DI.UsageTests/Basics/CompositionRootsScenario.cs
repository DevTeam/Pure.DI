/*
$v=true
$p=1
$d=Composition roots
$h=This example demonstrates several ways to create a composition root.
$h=> [!TIP]
$h=> There is no limit to the number of roots, but you should consider limiting the number of roots. Ideally, an application should have a single composition root.
$h=
$h=If you use classic DI containers, the composition is resolved dynamically every time you call a method similar to `T Resolve<T>()` or `object GetService(Type type)`. The root of the composition there is simply the root type of the composition of objects in memory `T` or `Type` type. There can be as many of these as you like. In the case of Pure.DI, the number of composition roots is limited because for each composition root a separate property or method is created at compile time. Therefore, each root is defined explicitly by calling the `Root(string rootName)` method.
$f=The name of the composition root is arbitrarily chosen depending on its purpose but should be restricted by the property naming conventions in C# since it is the same name as a property in the composition class. In reality, the _Root_ property has the form:
$f=```c#
$f=public IService Root
$f={
$f=  get
$f=  {
$f=    return new Service(new Dependency());
$f=  }
$f=}
$f=```
$f=To avoid generating _Resolve_ methods just add a comment `// Resolve = Off` before a _Setup_ method:
$f=```c#
$f=// Resolve = Off
$f=DI.Setup("Composition")
$f=  .Bind<IDependency>().To<Dependency>()
$f=  ...
$f=```
$f=This can be done if these methods are not needed, in case only certain composition roots are used. It's not significant then, but it will help save resources during compilation.
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