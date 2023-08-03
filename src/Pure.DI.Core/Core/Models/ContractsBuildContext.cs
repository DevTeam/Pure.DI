namespace Pure.DI.Core.Models;

internal readonly record struct ContractsBuildContext(
    in MdBinding Binding,
    object? ContextTag);