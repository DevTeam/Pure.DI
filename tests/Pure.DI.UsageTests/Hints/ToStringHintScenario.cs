/*
$v=true
$p=5
$d=ToString Hint
$h=The _ToString_ hint determines if the _ToString()_ method should be generated. This method provides a text-based class diagram in the format [mermaid](https://mermaid.js.org/). To see this diagram, just call the ToString method and copy the text to [this site](https://mermaid.live/). An example class diagram can be seen below.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable SuggestVarOrType_BuiltInTypes
namespace Pure.DI.UsageTests.Hints.ToStringHintScenario;

using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService { }

internal class Service : IService
{
    public Service(IDependency dependency) { }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // FormatCode = On
// {            
        DI.Setup("Composition")
            .Hint(Hint.ToString, "On")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("MyService");

        var composition = new Composition();
        string classDiagram = composition.ToString();
// }
        TestTools.SaveClassDiagram(composition, nameof(ToStringHintScenario));
    }
}