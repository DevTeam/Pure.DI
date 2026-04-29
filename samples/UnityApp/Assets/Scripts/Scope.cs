using Pure.DI;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable RequiredBaseTypesIsNotInherited
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable Unity.RedundantSerializeFieldAttribute
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedMember.Local

internal class ClocksComposition
{
    [SerializeField] private ClockConfig clockConfig;

    void Setup() => DI.Setup(kind: CompositionKind.Internal)
        .Transient(() => clockConfig)
        .Singleton<ClockService>();
}

public partial class Scope : MonoBehaviour
{
    [SerializeField] private Scope parentScope;

    private bool isReady;

    void Setup() => DI.Setup()
        .Hint(Hint.ScopeMethodName, "SetupScope")
        .DependsOn(nameof(ClocksComposition), SetupContextKind.Members)
        .Bind<IClockSession>().As(Lifetime.Scoped).To<ClockSession>()
        .Root<ClockManager>(nameof(ClockManager))
        .Builders<MonoBehaviour>();

    public void EnsureReady()
    {
        if (isReady)
        {
            return;
        }

        if (parentScope != null && !ReferenceEquals(parentScope, this))
        {
            parentScope.EnsureReady();
            SetupScope(parentScope, this);
        }

        isReady = true;
        Debug.Log($"Scene '{gameObject.scene.name}' Pure.DI scope is ready on '{name}'.");
    }

    void Awake()
    {
        EnsureReady();
    }

    void Start()
    {
        EnsureReady();
        ClockManager.Start();
    }

    void OnDestroy()
    {
        Dispose();
    }
}
