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
            .Bind<IClock>("abc").To<Clock>()
            .Root<IClock>("Clock");

        // When
        var composition = new AbcComposition();
        var a = composition.Clock;
        composition.Resolve<IClock>();
        composition.Resolve<IClock>("abc");

        // Then
    }
}