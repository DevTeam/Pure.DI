namespace Pure.DI.IntegrationTests.Owned;

public enum OwnedComparisonScenario
{
    DisposeAllFactoryHandles = 1,
    DisposeOnlyMiddleFactoryHandle,
    DisposeNestedInnerHandle,
    DisposeNestedOuterHandle,
    DisposeDiamondGraph,
    DisposeMiddleTripleNestedHandle,
    IsolateFactoryUnitsWithNestedHandles,
    DisposeMixedLifetimeGraph
}
