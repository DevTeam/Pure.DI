/*
$v=true
$p=9
$d=Customize the generated interface
$h=This example shows how to change the generated interface name, namespace, and accessibility.
$f=The example shows how to:
$f=- Generate an interface into a custom namespace
$f=- Rename the generated interface
$f=- Make the generated interface internal
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using Pure.DI;

namespace Contracts
{
    public partial interface IWorker;

    [GenerateInterface(namespaceName: "Contracts", interfaceName: "IWorker")]
    public class Worker : IWorker
    {
        public string Message => "custom";
    }
}

namespace Pure.DI.UsageTests.Interface.GenerateInterfaceCustomizationScenario
{
    using Pure.DI.UsageTests;
    using Contracts;
    using Pure.DI;
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
                .Bind().To<Worker>()
                .Root<App>(nameof(App));

            var composition = new Composition();
            var app = composition.App;

            app.Message.ShouldBe("custom");
            // }

            composition.SaveClassDiagram();
        }
    }

    public class App(IWorker worker)
    {
        public string Message { get; } = worker.Message;
    }
}
