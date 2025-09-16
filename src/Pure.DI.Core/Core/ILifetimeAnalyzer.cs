namespace Pure.DI.Core;

interface ILifetimeAnalyzer
{
    Lifetime GetActualDependencyLifetime(Lifetime targetLifetime, Lifetime dependencyLifetime);

    bool ValidateScopedToSingleton(Lifetime actualTargetLifetime, Lifetime dependencyLifetime);

    bool ValidateRootKindSpecificLifetime(Root root, Lifetime lifetime);
}