namespace Pure.DI.Core.Models;

internal record BuildContext(
    bool IsThreadSafe,
    IDictionary<MdBinding, Variable> Variables,
    LinesBuilder Code,
    bool IsRootContext = true,
    object? ContextTag = default);