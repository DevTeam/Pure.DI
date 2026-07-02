/*
$v=true
$p=2
$d=Root with name template
$h=Instead of a fixed root name, `Root<T>()` accepts a name template where the `{type}` placeholder is replaced with the dependency's type name. This is handy when you declare many roots and want them to follow a consistent naming convention: the template `"My{type}"` here produces a root property named `MyApiClient`.
$f=>[!NOTE]
$f=>Name templates provide flexibility in root naming but should be used consistently to maintain code readability.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeMadeStatic.Global
#pragma warning disable CS9113 // Parameter is unread.
#pragma warning disable CA1822

namespace Pure.DI.UsageTests.Advanced.RootWithNameTemplateScenario;

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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup("Composition")
            // The name template "My{type}" specifies that the root property name
            // will be formed by adding the prefix "My" to the type name "ApiClient".
            .Root<ApiClient>("My{type}");

        var composition = new Composition();

        // The property name is "MyApiClient" instead of "ApiClient"
        // thanks to the name template "My{type}"
        var apiClient = composition.MyApiClient;

        apiClient.GetProfile().ShouldBe("Content from https://example.com/profile");
        // }
        composition.SaveClassDiagram();
    }
}

// {
class NetworkClient
{
    public string Get(string uri) => $"Content from {uri}";
}

class ApiClient(NetworkClient client)
{
    public string GetProfile() => client.Get("https://example.com/profile");
}
// }