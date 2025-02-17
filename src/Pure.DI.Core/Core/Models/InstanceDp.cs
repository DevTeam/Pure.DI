namespace Pure.DI.Core.Models;

record InstanceDp(
    ImmutableArray<DpMethod> Methods,
    ImmutableArray<DpField> Fields,
    ImmutableArray<DpProperty> Properties);