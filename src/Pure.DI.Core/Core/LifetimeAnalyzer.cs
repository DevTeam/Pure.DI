namespace Pure.DI.Core;

internal class LifetimeAnalyzer : ILifetimeAnalyzer
{
    // Lifetimes sorted by potential state storage duration from least to greatest.
    private static readonly List<Lifetime> LifetimesByPriority =
    [
        Lifetime.Transient,
        Lifetime.PerBlock,
        Lifetime.PerResolve,
        Lifetime.Scoped,
        Lifetime.Singleton
    ];

    private static readonly int[] LifetimePriorities = new int[(int)LifetimesByPriority.Max() + 1];

    static LifetimeAnalyzer()
    {
        System.Diagnostics.Debug.Assert(
            LifetimesByPriority.Count == Enum.GetValues(typeof(Lifetime)).Length,
            "Some lifetime is not accounted for in the LifetimeAnalyzer class.");

        for (var priority = 0; priority < LifetimesByPriority.Count; priority++)
        {
            LifetimePriorities[(int)LifetimesByPriority[priority]] = priority;
        }
    }

    public Lifetime GetActualDependencyLifetime(Lifetime targetLifetime, Lifetime dependencyLifetime) =>
        LifetimePriorities[(int)targetLifetime] >= LifetimePriorities[(int)dependencyLifetime]
            ? targetLifetime
            : dependencyLifetime;

    public bool ValidateLifetimes(Lifetime actualTargetLifetime, Lifetime dependencyLifetime) =>
        !(actualTargetLifetime == Lifetime.Singleton && dependencyLifetime == Lifetime.Scoped);
}