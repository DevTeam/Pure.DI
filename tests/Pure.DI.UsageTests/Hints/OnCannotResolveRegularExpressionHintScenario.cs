/*
$v=true
$p=4
$d=OnCannotResolve regular expression hint
$sa=OnCannotResolve wildcard hint
$h=Hints are used to fine-tune code generation. The `OnCannotResolve` hint determines whether to generate a partial `OnCannotResolve<T>(...)` method to handle a scenario where an instance which cannot be resolved.
$h=This is the escape hatch for values the container cannot know: connection strings, secrets and options that come from a configuration file at startup, resolved once per request type instead of being threaded through every signature.
$h=In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// OnCannotResolveContractTypeNameRegularExpression = string`.
$f=The `OnCannotResolveContractTypeNameRegularExpression` hint helps define the set of types that require manual dependency resolution. You can use it to specify a regular expression to filter the full type name.
$f=The hook is a fallback of last resort: it is called only when no binding, argument or auto-binding satisfies the request, so an explicit `Bind` or `Arg` always wins and migrating away from the hook is incremental.
$f=Whatever the method returns becomes the injected value, so it must handle every type the filter admits - returning `default` for a struct hides mistakes, and throwing there surfaces the failure at the first resolution with a clear message.
$f=The regular expression is matched against the full type name, so anchoring it (`(.*Configuration|string)$`) keeps the hook from catching types you meant to bind.
$f=Prefer composition or root arguments for values that really are inputs, and keep this hook for cross-cutting defaults; the graph becomes easier to read when the values are visible in the setup.
$f=For more hints, see [this](../README.md#setup-hints) page.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Hints.OnCannotResolveRegularExpressionHintScenario;
#pragma warning disable CA1822
using Shouldly;
using Xunit;
using static Hint;

// {
//# using Pure.DI;
//# using static Pure.DI.Hint;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        // The "OnCannotResolveContractTypeNameRegularExpression" hint defines a regular expression
        // to filter the full type name of unresolved dependencies.
        // In this case, we want to manually handle only "string" types.
        // OnCannotResolveContractTypeNameRegularExpression = string
        DI.Setup(nameof(Composition))
            .Hint(OnCannotResolve, "On")
            .Bind().To<DatabaseAccess>()
            .Bind().To<BusinessService>()
            .Root<IBusinessService>("BusinessService");

        var composition = new Composition();
        var businessService = composition.BusinessService;

        // Check that the connection string was successfully injected via OnCannotResolve
        businessService.DatabaseAccess.ConnectionString.ShouldBe("Server=localhost;Database=MyDb;");

// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDatabaseAccess
{
    string ConnectionString { get; }
}

// A service requiring a connection string.
// The connection string is a primitive type 'string' that is not bound in the DI setup.
// It will be resolved via the 'OnCannotResolve' fallback method.
class DatabaseAccess(string connectionString) : IDatabaseAccess
{
    public string ConnectionString { get; } = connectionString;
}

interface IBusinessService
{
    IDatabaseAccess DatabaseAccess { get; }
}

class BusinessService(IDatabaseAccess databaseAccess) : IBusinessService
{
    public IDatabaseAccess DatabaseAccess { get; } = databaseAccess;
}

partial class Composition
{
    // This method is called when a dependency cannot be resolved by the standard DI.
    // It serves as a fallback mechanism.
    private partial T OnCannotResolve<T>(
        object? tag,
        Lifetime lifetime)
    {
        // Check if the requested type is a string (according to the hint filter)
        if (typeof(T) == typeof(string))
        {
            // Provide the configuration value (e.g., loaded from a file)
            return (T)(object)"Server=localhost;Database=MyDb;";
        }

        throw new InvalidOperationException("Cannot resolve " + typeof(T));
    }
}
// }