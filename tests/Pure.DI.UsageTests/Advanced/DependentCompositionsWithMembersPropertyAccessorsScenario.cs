/*
$v=true
$p=9
$d=Dependent compositions with setup context members and property accessors
$sa=Dependent compositions with setup context members
$h=This scenario shows how to copy referenced members and implement custom property accessors via partial methods.
$h=When this occurs: you need base setup properties with logic, but the dependent composition must remain parameterless.
$h=What it solves: keeps Unity-friendly composition while letting the user implement property logic.
$h=How it is solved in the example: uses DependsOn(..., SetupContextKind.Members) and implements partial get_ methods.
$h=The property itself is declared by the generator as a partial member, so the dependent composition decides what the getter does while the base setup keeps its own body.
$f=BaseComposition provides a connection string with a plain field-backed accessor and a max connections property with logic; the dependent composition implements `get__MaxConnections` and leaves `ConnectionString` alone.
$f=This is the escape hatch for a host that builds the composition from a default constructor but where some configuration needs normalization - clamping, defaults, unit conversion - before it reaches a service.
$f=Accessor logic is not copied; the user provides partial implementations, and a property whose accessor is not overridden keeps the copied member.
$f=Simple property accessors (field-backed) can be used without partial methods.
$f=Prefer copying values through `SetupContextKind.Argument` when the accessor only needs to read another object: partial accessors are per composition and are easy to forget when the base property changes.
$f=
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedMember.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ConvertToAutoProperty
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.DependentCompositionsWithMembersPropertyAccessorsScenario;

using Pure.DI;
using UsageTests;
using Shouldly;
using Xunit;
using static CompositionKind;

// {
//# using Pure.DI;
//# using static Pure.DI.CompositionKind;
// }

public class Scenario
{
	[Fact]
	public void Run()
	{
		// Resolve = Off
		// {
		var composition = new Composition
		{
			ConnectionString = "Server=prod-db.example.com;Database=AppDb;"
		};

		var service = composition.DatabaseService;
		// }

		// ConnectionString uses simple field-backed accessor
		service.ConnectionString.ShouldBe("Server=prod-db.example.com;Database=AppDb;");

		// MaxConnections uses custom accessor logic (returns configured value + 1)
		service.MaxConnections.ShouldBe(101);

		composition.SaveClassDiagram();
	}
}

// {
interface IDatabaseService
{
	string ConnectionString { get; }
	int MaxConnections { get; }
}

class DatabaseService(
	[Tag("connectionString")] string connectionString,
	[Tag("maxConnections")] int maxConnections) : IDatabaseService
{
	public string ConnectionString { get; } = connectionString;
	public int MaxConnections { get; } = maxConnections;
}

/// <summary>
/// Base composition providing database configuration properties
/// </summary>
internal partial class BaseComposition
{
	/// <summary>
	/// Connection string - simple property with field-backed accessor (no custom logic)
	/// </summary>
	public string ConnectionString { get; set; } = "";

	/// <summary>
	/// Maximum number of connections - property with custom getter logic
	/// </summary>
	private int _maxConnections = 100;

	public int MaxConnections
	{
		get => _maxConnections;
		set => _maxConnections = value;
	}

	private void Setup()
	{
		DI.Setup(nameof(BaseComposition), Internal)
			.Bind<string>("connectionString").To(_ => ConnectionString)
			.Bind<int>("maxConnections").To(_ => MaxConnections);
	}
}

/// <summary>
/// Dependent composition implementing custom accessor logic for properties
/// </summary>
internal partial class Composition
{
	/// <summary>
	/// MaxConnections backing field
	/// </summary>
	private int _maxConnections = 100;

	/// <summary>
	/// Custom accessor logic: returns configured value + 1 to ensure minimum buffer
	/// </summary>
	private partial int get__MaxConnections() => _maxConnections + 1;

	/// <summary>
	/// Setter for MaxConnections
	/// </summary>
	public void SetMaxConnections(int value) => _maxConnections = value;

	private void Setup()
	{
		DI.Setup(nameof(Composition))
			.DependsOn(nameof(BaseComposition), SetupContextKind.Members)
			.Bind<IDatabaseService>().To<DatabaseService>()
			.Root<IDatabaseService>("DatabaseService");
	}
}
// }
