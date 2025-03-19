// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

readonly record struct DpOverride(
    MdOverride Source,
    ImmutableArray<Injection> Injections);