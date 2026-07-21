namespace Pure.DI.IntegrationTests.Owned;

public sealed class OwnedComparisonTests
{
    public static TheoryData<OwnedComparisonScenario> Scenarios =>
        new(Enum.GetValues<OwnedComparisonScenario>());

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task ShouldMatchAutofacOwnedBehavior(OwnedComparisonScenario scenario)
    {
        // Given
        var expected = AutofacOwnedRunner.Run(scenario);

        // When
        var actual = await PureDiOwnedRunner.RunAsync(scenario);

        // Then
        actual.ShouldBe(expected);
    }
}
