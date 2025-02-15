namespace Pure.DI.Core.Models;

internal record InstanceDp(
    ImmutableArray<DpMethod> Methods,
    ImmutableArray<DpField> Fields,
    ImmutableArray<DpProperty> Properties);