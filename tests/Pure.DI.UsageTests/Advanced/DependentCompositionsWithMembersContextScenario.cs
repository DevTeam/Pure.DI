/*
$v=true
$p=7
$d=Dependent compositions with setup context members
$h=This scenario shows how to copy referenced members from a base setup into the dependent composition.
$h=When this occurs: you want to reuse base setup state without passing a separate context instance.
$h=What it solves: lets dependent compositions access base setup members directly (Unity-friendly, no constructor args).
$h=How it is solved in the example: uses DependsOn(..., SetupContextKind.Members) and sets members on the composition instance. The name parameter is optional; methods are declared partial and implemented by the user.
$f=
$f=What it shows:
$f=- Setup context members copied into the dependent composition.
$f=- Realistic scenario: configuring database connection and logging for a data service.
$f=
$f=Important points:
$f=- The composition remains parameterless and can be configured via its own members.
$f=
$f=Example demonstrates:
$f=  1. BaseComposition provides database connection settings, default timeout, and a protected diagnostics field
$f=  2. Dependent Composition adds logging level and max retries configuration
$f=  3. DataService uses all settings including the protected field for conditional output
$f=  4. Protected field 'EnableDiagnostics' from BaseComposition is accessible in Composition via DependsOn
$f=  5. Composition's constructor sets EnableDiagnostics to true to enable detailed status
$f=
$f=Note: SetupContextKind.Members copies both public and protected members to the dependent composition.
$f=
$f=Useful when:
$f=- Base setup has instance members initialized by the host or framework.
$f=- You need to extend configuration with additional settings in derived compositions.
$f=
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable ConvertToConstant.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.DependentCompositionsWithMembersContextScenario;

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
            ConnectionSettings = new DatabaseConnectionSettings("prod-db.example.com", 5432, "app_database"),
            LogLevel = "Info",
            MaxRetries = 5
        };

        var service = composition.DataService;
        // }

        // Verify that the service correctly uses settings from both base and dependent composition
        // Since EnableDiagnostics is set to true in Composition's constructor, full status is returned
        service.GetStatus().ShouldBe(
            "Database: app_database@prod-db.example.com:5432, " +
            "LogLevel: Info, " +
            "MaxRetries: 5, " +
            "Timeout: 5000ms");

        composition.SaveClassDiagram();
    }
}

// {
interface IDataService
{
    string GetStatus();
}

/// <summary>
/// Data service using connection settings and logging configuration
/// </summary>
class DataService(
    IDatabaseConnectionSettings connectionSettings,
    [Tag("logLevel")] string logLevel,
    [Tag("maxRetries")] int maxRetries,
    [Tag("timeout")] int timeoutMs,
    [Tag("enableDiagnostics")] bool enableDiagnostics) : IDataService
{
    public string GetStatus() => enableDiagnostics
        ? $"Database: {connectionSettings.DatabaseName}@{connectionSettings.Host}:{connectionSettings.Port}, " +
          $"LogLevel: {logLevel}, " +
          $"MaxRetries: {maxRetries}, " +
          $"Timeout: {timeoutMs}ms"
        : "OK";
}

/// <summary>
/// Base composition providing database connection settings, default timeout, and diagnostics flag
/// </summary>
internal partial class BaseComposition
{
    /// <summary>
    /// Enable detailed diagnostics logging (protected field accessible in derived compositions via DependsOn)
    /// </summary>
    protected bool EnableDiagnostics = false;

    public DatabaseConnectionSettings ConnectionSettings { get; set; } = new("", 0, "");

    private int GetDefaultTimeout() => 5000;

    private void Setup()
    {
        DI.Setup(nameof(BaseComposition), Internal)
            .Bind<IDatabaseConnectionSettings>().To(_ => ConnectionSettings)
            .Bind("enableDiagnostics").To(_ => EnableDiagnostics)
            .Bind("timeout").To(_ => GetDefaultTimeout());
    }
}

/// <summary>
/// Dependent composition extending the base with logging level and max retries, and enabling diagnostics
/// </summary>
internal partial class Composition
{
    /// <summary>
    /// Constructor enables diagnostics by default
    /// </summary>
    public Composition() => EnableDiagnostics = true;

    /// <summary>
    /// Application logging level
    /// </summary>
    public string LogLevel { get; set; } = "Warning";

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    private void Setup()
    {
        DI.Setup(nameof(Composition))
            .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
            .Bind<string>("logLevel").To(_ => LogLevel)
            .Bind<int>("maxRetries").To(_ => MaxRetries)
            .Bind<IDataService>().To<DataService>()
            .Root<IDataService>("DataService");
    }

    /// <summary>
    /// Implementation of partial method from base composition
    /// </summary>
    private partial int GetDefaultTimeout() => 5000;
}

/// <summary>
/// Database connection settings
/// </summary>
record DatabaseConnectionSettings(string Host, int Port, string DatabaseName) : IDatabaseConnectionSettings;

/// <summary>
/// Database connection settings interface
/// </summary>
interface IDatabaseConnectionSettings
{
    string Host { get; }

    int Port { get; }

    string DatabaseName { get; }
}
// }
