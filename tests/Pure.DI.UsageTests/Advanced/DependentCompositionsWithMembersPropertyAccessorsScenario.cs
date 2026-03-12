/*
$v=true
$p=7
$d=Dependent compositions with setup context members and property accessors
$h=This scenario shows how to copy referenced members and implement custom property accessors via partial methods.
$h=When this occurs: you need base setup properties with logic, but the dependent composition must remain parameterless.
$h=What it solves: keeps Unity-friendly composition while letting the user implement property logic.
$h=How it is solved in the example: uses DependsOn(..., SetupContextKind.Members) and implements partial get_ methods.
$f=
$f=What it shows:
$f=- Custom property logic via partial accessor methods.
$f=- Properties with simple field-backed accessors (no logic).
$f=
$f=Important points:
$f=- Accessor logic is not copied; the user provides partial implementations.
$f=- Simple property accessors (field-backed) can be used without partial methods.
$f=
$f=Example demonstrates:
$f= 1. BaseComposition provides connection string and max connections properties
$f= 2. ConnectionString has simple field-backed accessor (no logic)
$f= 3. MaxConnections has custom getter logic via partial method
$f= 4. Dependent Composition implements custom accessor logic for MaxConnections
$f=
$f=Useful when:
$f=- Properties include custom logic and are referenced by bindings in a dependent setup.
$f=- Some properties are simple field-backed while others have custom logic.
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
