namespace Pure.DI.Core.Models;

readonly record struct ContractsBuildContext(
    in MdBinding Binding,
    object? ContextTag);