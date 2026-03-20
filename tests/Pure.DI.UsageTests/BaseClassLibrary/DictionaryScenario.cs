namespace Pure.DI.UsageTests.BCL.DictionaryScenario;

using AsyncEnumerableScenario;

public class DictionaryScenario
{
    [Fact]
    public async Task Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind(Tag.Unique).To((DependencyA dep) => new KeyValuePair<string, IDependency>("A", dep))
            .Bind(Tag.Unique).To((DependencyB dep) => new KeyValuePair<string, IDependency>("B", dep))

            // Composition root
            .Root<Service>("MyService");

        var composition = new Composition();
        var myService = composition.MyService;

        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class DependencyA: IDependency;

class DependencyB: IDependency;

 #pragma warning disable CS9113 // Parameter is unread.
class Service(IDictionary<string, IDependency> dependencies)
 #pragma warning restore CS9113 // Parameter is unread.
{
}


// }