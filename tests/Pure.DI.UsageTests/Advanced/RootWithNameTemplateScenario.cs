/*
$v=true
$p=1
$d=Root with name template
$sa=Composition roots
$h=Instead of a fixed root name, `Root<T>()` accepts a name template where the `{type}` placeholder is replaced with the dependency's type name; `{TYPE}` gives the fully qualified name and `{tag}` the first root tag. This is handy when you declare many roots and want them to follow a consistent naming convention: the template `"My{type}"` here produces a root property named `MyApiClient`.
$h=The result is made a valid C# identifier, so a generic type name is usable in a template.
$f=Use it with `Roots<T>("Root{type}")` ([roots with filter](roots-with-filter.md)) when the same convention must cover many types, so adding a type does not mean adding a hand-written root.
$f=The generated name is part of the public API: changing the template renames members for every consumer, so treat it like any other breaking change.
$f=Two templates that resolve to the same name are reported as `DIE005`.
$f=>[!NOTE]
$f=>Name templates provide flexibility in root naming but should be used consistently to maintain code readability.
$f=
$f=Prefer an explicit name when the root is part of a contract consumed by other code, and a template for the mechanically generated set.
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