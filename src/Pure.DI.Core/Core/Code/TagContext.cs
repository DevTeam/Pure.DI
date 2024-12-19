namespace Pure.DI.Core.Code;

internal record TagContext(
    IReadOnlyCollection<CompositionCode> Compositions);