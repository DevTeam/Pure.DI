namespace Pure.DI.Core.Models;

internal record TagContext(
    IReadOnlyCollection<CompositionCode> Compositions);