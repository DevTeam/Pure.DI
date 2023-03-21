// Abc = xyz
namespace Pure.DI.UsageTests;

using Models;

public class AbcTests
{
    [Fact]
    public void Should()
    {
        // Given
        DI.Setup("Pure.DI.UsageTests.AbcComposer")
            .Bind<IClock>().To<Clock>()
            .Root<IClock>("Clock");

        // When
        var a = new AbcComposer().Clock;

        // Then
    }
}