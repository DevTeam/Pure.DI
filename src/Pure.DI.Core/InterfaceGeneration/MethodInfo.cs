namespace Pure.DI.InterfaceGeneration;

readonly record struct MethodInfo(
    string Name,
    string ReturnType,
    string Documentation,
    ImmutableArray<string> Parameters,
    ImmutableArray<(string Arg, string WhereConstraint)> GenericArgs);