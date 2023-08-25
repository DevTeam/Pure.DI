namespace Pure.DI.Core.Models;

internal record BuildContext(
    IDictionary<MdBinding, Variable> Variables,
    LinesBuilder Code,
    IVarIdGenerator IdGenerator,
    bool IsRootContext = true,
    object? ContextTag = default);