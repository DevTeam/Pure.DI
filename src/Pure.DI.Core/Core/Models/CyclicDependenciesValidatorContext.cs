namespace Pure.DI.Core.Models;

record CyclicDependenciesValidatorContext(
    DependencyGraph DependencyGraph,
    HashSet<object> Errors);