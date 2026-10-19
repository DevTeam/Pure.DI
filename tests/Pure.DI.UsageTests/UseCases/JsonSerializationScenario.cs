/*
$v=true
$p=2
$d=JSON serialization
$sa=Generics
$h=Serialization can be hidden behind injectable functions instead of scattering `JsonSerializer` calls across the code. A shared `JsonSerializerOptions` instance is bound as a singleton, and two generic bindings tagged `JSON` expose `Func<string, TT?>` and `Func<TT, string>` delegates that deserialize and serialize any type using those options. `SettingsService` then just injects these functions to load and save its `Settings`, keeping it decoupled from the serializer.
$f=>[!TIP]
$f=>Binding delegates with generic type markers like `TT` produces a serialize/deserialize function for every type where it is injected — no per-type bindings are needed.
$f=
$f=This is how application code stores settings, caches payloads or talks to a queue without depending on a particular serializer package. The options object is a singleton, so the serializer's metadata cache is built once and reused for every type.
$f=The delegates are resolved per consumer, so the serializer never becomes a static and a test can replace one delegate without replacing a whole serialization stack.
$f=>[!WARNING]
$f=>A `JsonSerializerOptions` instance must not be modified after it has been used for serialization - the serializer caches metadata per instance. Configure it completely inside the binding, as here.
$f=
$f=Prefer injectable delegates over a static `JsonSerializer` call when the format may change or must be faked in tests. The same shape works for any library that needs configuration - compare [AutoMapper](automapper.md) for mapping and [Serilog](serilog.md) for logging.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable WithExpressionModifiesAllMembers
#pragma warning disable CS9113 // Parameter is unread.
#pragma warning disable CA1859
namespace Pure.DI.UsageTests.UseCases.JsonSerializationScenario;

#pragma warning disable CA2263
using System.Text.Json;
using Xunit;
using static Lifetime;
using static Tag;

// {
//# using Pure.DI;
//# using System.Text.Json;
//# using static Pure.DI.Lifetime;
//# using static Pure.DI.Tag;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition();
        var settings = composition.Settings;
        settings.Size.ShouldBe(10);

        settings.Size = 99;
        settings.Size.ShouldBe(99);

        settings.Size = 33;
        settings.Size.ShouldBe(33);
// }
        composition.SaveClassDiagram();
    }
}

// {
record Settings(int Size)
{
    public static readonly Settings Default = new(10);
}

interface IStorage
{
    void Save(string data);

    string? Load();
}

class Storage : IStorage
{
    private string? _data;

    public void Save(string data) => _data = data;

    public string? Load() => _data;
}

interface ISettingsService
{
    int Size { get; set; }
}

class SettingsService(
    [Tag(JSON)] Func<string, Settings?> deserialize,
    [Tag(JSON)] Func<Settings, string> serialize,
    IStorage storage)
    : ISettingsService
{
    public int Size
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        get => GetSettings().Size;
        set => SaveSettings(GetSettings() with { Size = value });
    }

    private Settings GetSettings() =>
        storage.Load() is {} data && deserialize(data) is {} settings
            ? settings
            : Settings.Default;

    private void SaveSettings(Settings settings) =>
        storage.Save(serialize(settings));
}

partial class Composition
{
    private void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Root<ISettingsService>(nameof(Settings))
            .Bind().To<SettingsService>()
            .DefaultLifetime(Singleton)
            .Bind().To(() => new JsonSerializerOptions { WriteIndented = true })
            .Bind(JSON).To<JsonSerializerOptions, Func<string, TT?>>(options => json => JsonSerializer.Deserialize<TT>(json, options))
            .Bind(JSON).To<JsonSerializerOptions, Func<TT, string>>(options => value => JsonSerializer.Serialize(value, options))
            .Bind().To<Storage>();
}
// }