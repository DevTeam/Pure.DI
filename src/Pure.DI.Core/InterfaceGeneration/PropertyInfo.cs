namespace Pure.DI.InterfaceGeneration;

readonly record struct PropertyInfo(
    string Name,
    string Type,
    bool HasGet,
    PropertySetKind SetKind,
    bool IsRef,
    string Documentation);