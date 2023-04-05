namespace Pure.DI.Core.Models;

internal record struct ContractsBuildContext(
    in MdBinding Binding,
    object? ContextTag);