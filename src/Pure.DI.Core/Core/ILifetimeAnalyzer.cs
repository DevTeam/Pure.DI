namespace Pure.DI.Core;

internal interface ILifetimeAnalyzer
{
    Lifetime GetActualDependencyLifetime(Lifetime targetLifetime, Lifetime dependencyLifetime);

    bool ValidateLifetimes(Lifetime actualTargetLifetime, Lifetime dependencyLifetime);
}