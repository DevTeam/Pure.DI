namespace Pure.DI.UsageTests;

using Models;

public class AbcTests
{
    [Fact]
    public void Should()
    {
        // Given
        DI.Setup("AbcComposer")
            .Bind<IClock>().To<Clock>()
            .Root<IClock>("Clock");

        // When
        var a = new AbcComposer().Clock;

        // Then
    }
}