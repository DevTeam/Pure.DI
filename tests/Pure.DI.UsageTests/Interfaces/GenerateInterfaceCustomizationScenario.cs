/*
$v=true
$p=4
$d=Customize the generated interface
$h=This example shows how to place a generated contract in a dedicated Contracts namespace.
$f=The example shows how to:
$f=- Generate an interface into a custom namespace
$f=- Rename the generated interface
$f=- Keep the contract separate from implementation details
$i=false
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using Pure.DI;

namespace Pure.DI.UsageTests.Interfaces.GenerateInterfaceCustomizationScenario
{
    using Contracts;
    using Xunit;

    public class Scenario
    {
        [Fact]
        public void Run()
        {
            // {
            DI.Setup(nameof(Composition))
                .Bind().To<InvoiceGenerator>()
                .Root<App>(nameof(App));

            var composition = new Composition();
            var app = composition.App;

            app.InvoiceId.ShouldBe("INV-0042");
            // }

            composition.SaveClassDiagram();
        }
    }

    // {
    public class App(IMyInvoiceGenerator generator)
    {
        public string InvoiceId { get; } = generator.Format(42);
    }
    // }
}

// {
namespace Contracts
{
    public partial interface IMyInvoiceGenerator;

    [GenerateInterface(namespaceName: "Contracts", interfaceName: nameof(IMyInvoiceGenerator))]
    public class InvoiceGenerator : IMyInvoiceGenerator
    {
        public string Format(int number) => $"INV-{number:0000}";
    }
}
// }