/*
$v=true
$p=3
$i=false
$d=Unity scene scopes
$h=Demonstrates Unity-style scoped lifetime boundaries where Unity creates MonoBehaviour instances and Pure.DI builds them up without constructors.
$h=Each loaded scene has its own scope, so scoped services are shared inside one scene and isolated from another scene.
$f=>[!NOTE]
$f=>In a real Unity project the scene objects are created by Unity. The sample uses constructors only to simulate serialized references in a test.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertConstructorToMemberInitializers
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Pure.DI.UsageTests.Unity.UnitySceneScopesScenario;

using Shouldly;
using UnityEngine;
using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using UnityEngine;
//# using static Pure.DI.Lifetime;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        // Application scope: one root for shared singletons.
        var application = new Scope("Application");

        // Unity loads two scenes. Each scene has its own MonoBehaviour scope object.
        var menuScene = Scope.SetupScope(application, new Scope("Menu"));
        var levelScene = Scope.SetupScope(application, new Scope("Level"));

        // Unity creates MonoBehaviour instances. Pure.DI only builds them up.
        var menuClock1 = new Clock(menuScene);
        var menuClock2 = new Clock(menuScene);
        var levelClock = new Clock(levelScene);

        menuClock1.Awake();
        menuClock2.Awake();
        levelClock.Awake();

        // Same scene => same scoped dependency.
        menuClock1.Session.ShouldBe(menuClock2.Session);

        // Different scenes => different scoped dependencies.
        menuClock1.Session.ShouldNotBe(levelClock.Session);

        // Singleton dependency is still shared from the application scope.
        menuClock1.ClockService.ShouldBe(levelClock.ClockService);

        menuScene.Dispose();
        menuClock1.Session.IsDisposed.ShouldBeTrue();
        levelClock.Session.IsDisposed.ShouldBeFalse();

        levelScene.Dispose();
        levelClock.Session.IsDisposed.ShouldBeTrue();

        application.Dispose();
        menuClock1.ClockService.IsDisposed.ShouldBeTrue();
// }
    }
}

// {
public class Clock : MonoBehaviour
{
    [SerializeField] Scope scope;
// }
    public Clock(Scope scope)
    {
        this.scope = scope;
    }
// {
    [Dependency]
    public IClockService ClockService { get; set; }

    [Dependency]
    public IClockSession Session { get; set; }

    public void Awake()
    {
        scope.BuildUp(this);
    }
}

public interface IClockConfig
{
    TimeSpan Offset { get; }
}

[CreateAssetMenu(fileName = "ClockConfig", menuName = "Clock/Config")]
public class ClockConfig : ScriptableObject, IClockConfig
{
    [SerializeField] int offsetHours;
// }
    public ClockConfig()
    {
        offsetHours = 3;
    }
// {
    public TimeSpan Offset => TimeSpan.FromHours(offsetHours);
}

public interface IClockService
{
    DateTime Now { get; }

    bool IsDisposed { get; }
}

public class ClockService(IClockConfig config) : IClockService, IDisposable
{
    public DateTime Now => DateTime.UtcNow + config.Offset;

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public interface IClockSession
{
    string SceneName { get; }

    bool IsDisposed { get; }
}

public class ClockSession([Tag("scene name")] string sceneName) : IClockSession, IDisposable
{
    public string SceneName { get; } = sceneName;

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public partial class Scope : MonoBehaviour
{
    [SerializeField] ClockConfig clockConfig;
    [SerializeField] string sceneName;
// }
    public Scope(string sceneName)
    {
        clockConfig = new ClockConfig();
        this.sceneName = sceneName;
    }
    // Resolve = Off
    // ToString = Off
// {
    void Setup() => DI.Setup()
        .Hint(Hint.ScopeMethodName, "SetupScope")
        .Bind().To(() => clockConfig)
        .Bind("scene name").To(_ => sceneName)
        .Bind<IClockService>().As(Singleton).To<ClockService>()
        .Bind<IClockSession>().As(Scoped).To<ClockSession>()
        .Builders<MonoBehaviour>();

    void OnDestroy()
    {
        Dispose();
    }
}
// }
