/*
$v=true
$p=2
$d=Root with name template
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
        // This hint indicates to not generate methods such as Resolve
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