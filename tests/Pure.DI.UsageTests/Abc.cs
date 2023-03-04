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
            .Bind<string>().To(_ => "Xyz")
            .Root<IClock>("Clock")
            .Arg<string>("abc", 1)
            .Root<int>("Abc")
            .Root<string>("Xyz");

        // When
        var a = new AbcComposer("aaa").Clock;

        // Then
    }
}