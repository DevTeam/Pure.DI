/*
$v=true
$p=2
$i=false
$d=Unity with prefabs
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
// ReSharper disable ReplaceWithFieldKeyword
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Pure.DI.UsageTests.Unity.UnityWithPrefabsScenario;

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

public class ClockDigital : MonoBehaviour
{
    [SerializeField] private Text timeText;

    [Dependency] public IClockService ClockService { private get; set; }

    void FixedUpdate()
    {
        var now = ClockService.Now;
        timeText.text = now.ToString("HH:mm:ss");
    }
}

public interface IClockConfig
{
    TimeSpan Offset { get; }

    bool ShowDigital { get; }

    ClockDigital ClockDigitalPrefab { get; }
}

[CreateAssetMenu(fileName = "ClockConfig", menuName = "Clock/Config")]
public class ClockConfig : ScriptableObject, IClockConfig
{
    [SerializeField] int offsetHours;
    [SerializeField] bool showDigital;
    [SerializeField] ClockDigital clockDigitalPrefab;
// }
    public ClockConfig()
    {
        offsetHours = 3;
        showDigital = true;
        clockDigitalPrefab = new ClockDigital();
    }
// {
    public TimeSpan Offset => TimeSpan.FromHours(offsetHours);

    public bool ShowDigital => showDigital;

    public ClockDigital ClockDigitalPrefab => clockDigitalPrefab;
}

public interface IClockService
{
    DateTime Now { get; }
}

public class ClockService : IClockService, IDisposable
{
    private readonly IClockConfig _config;

    public ClockService(IClockConfig config)
    {
        _config = config;
    }

    public DateTime Now => DateTime.UtcNow + _config.Offset;

    public void Dispose()
    {
        // Perform any necessary cleanup here
    }
}

public class ClockManager : IDisposable
{
    private readonly Scope _scope;
    private readonly IClockConfig _config;

    public ClockManager(Scope scope, IClockConfig config)
    {
        _scope = scope;
        _config = config;
    }

    public void Start()
    {
        if (_config.ShowDigital)
        {
            _scope.BuildUp(Object.Instantiate(_config.ClockDigitalPrefab));
        }
    }

    public void Dispose()
    {
        // Perform any necessary cleanup here
    }
}

public partial class Scope : MonoBehaviour
{
    [SerializeField]  ClockConfig clockConfig;
// }
    public Scope()
    {
        clockConfig = new ClockConfig();
    }
    // Resolve = Off
    // ToString = Off
// {
    void Setup() => DI.Setup()
        .Bind().To(() => clockConfig)
        .Bind().As(Lifetime.Singleton).To<ClockService>()
        .Root<ClockManager>(nameof(ClockManager))
        .Builders<MonoBehaviour>();

    void Start()
    {
        ClockManager.Start();
    }

    void OnDestroy()
    {
        Dispose();
    }
}
// }