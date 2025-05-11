namespace Clock.Tests;

public class CurrentThreadDispatcherTests
{
    [Fact]
    public void ShouldDispatch()
    {
        // Given
        var dispatcher = new CurrentThreadDispatcher();
        var dispatched = false;

        // When
        dispatcher.Dispatch(() => dispatched = true);

        // Then
        dispatched.ShouldBeTrue();
    }
}