﻿/*
$v=true
$p=2
$i=false
$d=Unity MonoBehaviours
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
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Pure.DI.UsageTests.Unity.UnityAbcScenario;

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
        // Unity creates MonoBehaviour and start it
        var clock = new Clock();
        clock.Start();
        var otherClock = new OtherClock();
        otherClock.Start();
        Composition.Shared.SaveClassDiagram();
    }
}

// {
public class Clock : MonoBehaviour
{
    private const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;

    [SerializeField]
    private Transform hoursPivot;
    
    [SerializeField]
    private Transform minutesPivot;

    [SerializeField]
    private Transform secondsPivot;

    [Dependency]
    public IClockService ClockService { private get; set; }

    public void Start()
    {
        // Injects dependencies
        Composition.Shared.BuildUp(this);
    }

    public void Update()
    {
        var now = ClockService.Now.TimeOfDay;

        hoursPivot.localRotation = Quaternion
            .Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);
        
        minutesPivot.localRotation = Quaternion
            .Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);

        secondsPivot.localRotation = Quaternion
            .Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
    }
}

public class OtherClock : MonoBehaviour
{
    [Dependency]
    public IClockService ClockService { private get; set; }

    public void Start()
    {
        // Injects dependencies
        Composition.Shared.BuildUp(this);
    }

    public void Update()
    {
        // ReSharper disable once UnusedVariable
        var now = ClockService.Now.TimeOfDay;
    }
}

public interface IClockService
{
    DateTime Now { get; }
}

public class ClockService : IClockService
{
    public DateTime Now => DateTime.Now;
}

internal partial class Composition
{
    public static readonly Composition Shared = new();
    
    private void Setup() =>
// }
        // Resolve = off
// {
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<ClockService>()
            // Creates a builder for each type inherited from MonoBehaviour.
            // These types must be available at this point in the code.
            .Builders<MonoBehaviour>();
}
// }