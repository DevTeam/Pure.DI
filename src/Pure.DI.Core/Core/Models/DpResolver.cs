// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

record DpResolver(
    in MdResolver Source,
    Injection Injection,
    ImmutableArray<DpOverride> Overrides);