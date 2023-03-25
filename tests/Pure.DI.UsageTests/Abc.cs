namespace Pure.DI.UsageTests;

using Models;

public class AbcTests
{
    [Fact]
    public void Should()
    {
        // Given
        DI.Setup("AbcComposition")
            .Bind<string>().To(_ => "Xyz")
            .Bind<IClock>().To<Clock>()
            .Bind<IClock>("abc").To<Clock>()
            .Root<IClock>("Clock");

        // When
        var composition = new AbcComposition();

        // Then
        // ReSharper disable once NotAccessedVariable
        var clock = composition.Clock;
        clock= composition.Resolve<IClock>();
        clock = composition.Resolve<IClock>("abc");
        clock = (IClock)composition.Resolve(typeof(IClock));
        clock = (IClock)composition.Resolve(typeof(IClock), "abc");
    }
}