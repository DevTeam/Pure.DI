namespace Pure.DI.Core;

internal record InstanceDp(
    ImmutableArray<DpMethod> Methods,
    ImmutableArray<DpField> Fields,
    ImmutableArray<DpProperty> Properties);