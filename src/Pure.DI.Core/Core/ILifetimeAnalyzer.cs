namespace Pure.DI.Core;

interface ILifetimeAnalyzer
{
    Lifetime GetActualDependencyLifetime(Lifetime targetLifetime, Lifetime dependencyLifetime);

    bool ValidateLifetimes(Lifetime actualTargetLifetime, Lifetime dependencyLifetime);
}