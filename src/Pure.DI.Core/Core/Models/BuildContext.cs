namespace Pure.DI.Core.Models;

internal record BuildContext(
    IDictionary<MdBinding, Variable> Variables,
    LinesBuilder Code,
    bool IsRootContext = true);