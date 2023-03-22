namespace Pure.DI.UsageTests;

using Models;

public class AbcTests
{
    [Fact]
    public void Should()
    {
        // Given
        DI.Setup("AbcComposition")
            .Bind<IClock>().To<Clock>()
            .Root<IClock>("Clock");

        // When
        var a = new AbcComposition().Clock;

        // Then
    }
}