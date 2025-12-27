/*
$v=true
$p=1
$i=false
$d=Unity Basics
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
namespace Pure.DI.UsageTests.Unity.UnityBasicScenario;

using UnityEngine;
using Xunit;
using Quaternion = UnityEngine.Quaternion;

// {
//# using Pure.DI;
//# using UnityEngine;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // The unity engine creates MonoBehaviour
        var clock = new Clock();

        // And start it
        clock.Awake();
        clock.Update();
    }
}

// {
public class Clock : MonoBehaviour
{
    const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;
    [SerializeField] Scope scope;
    [SerializeField] Transform hoursPivot;
    [SerializeField] Transform minutesPivot;
    [SerializeField] Transform secondsPivot;
// }
    public Clock()
    {
        scope = new Scope();
        hoursPivot = new Transform();
        minutesPivot = new Transform();
        secondsPivot = new Transform();
    }
// {
    [Dependency]
    public IClockService ClockService { private get; set; }

    public void Awake()
    {
        scope.BuildUp(this);
    }

    public void Update()
    {
        var now = ClockService.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
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
}

public class ClockService : IClockService, IDisposable
{
    private readonly IClockConfig _config;

    public DateTime Now => DateTime.UtcNow + _config.Offset;

    public ClockService(IClockConfig config)
    {
        _config = config;
    }

    public void Dispose()
    {
        // Perform any necessary cleanup here
    }
}

public partial class Scope : MonoBehaviour
{
    [SerializeField] ClockConfig clockConfig;
// }
    public Scope()
    {
        clockConfig = new ClockConfig();
    }
    // Resolve = Off
    // ToString = Off
// {
    void Setup() =>
        DI.Setup()
        .Bind().To(() => clockConfig)
        .Bind().As(Lifetime.Singleton).To<ClockService>()
        .Builders<MonoBehaviour>();

    void OnDestroy()
    {
        Dispose();
    }
}
// }