namespace Pure.DI.UsageTests;

using Abstractions;
using Shouldly;
using Xunit;

public sealed class OwnTests
{
    [Fact]
    public void ShouldKeepNestedOwnershipGraphsIndependent()
    {
        var composition = new OwnComposition();
        var firstOuter = composition.Root;
        var firstOuterValue = firstOuter.Value;
        var firstInner = firstOuterValue.Inner;

        firstInner.Dispose();

        firstInner.Value.IsDisposed.ShouldBeTrue();
        firstOuterValue.IsDisposed.ShouldBeFalse();

        var secondOuter = composition.Root;
        var secondOuterValue = secondOuter.Value;
        var secondInner = secondOuterValue.Inner;

        secondOuter.Dispose();

        secondOuterValue.IsDisposed.ShouldBeTrue();
        secondInner.Value.IsDisposed.ShouldBeFalse();

        secondInner.Dispose();
        secondInner.Value.IsDisposed.ShouldBeTrue();
        firstOuter.Dispose();
    }

    [Fact]
    public void ShouldDisposeRejectedResourceAndThrowObjectDisposedException()
    {
        var owner = new Own();
        owner.Dispose();
        var resource = new OwnResource();

        Should.Throw<ObjectDisposedException>(() => owner.Add(resource));

        resource.IsDisposed.ShouldBeTrue();
    }

}

sealed class OwnResource : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

sealed class OwnOuter(Own<OwnResource> inner) : IDisposable
{
    public Own<OwnResource> Inner { get; } = inner;

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

partial class OwnComposition
{
    private static void Setup() =>
        DI.Setup(nameof(OwnComposition))
            .Bind().To<OwnResource>()
            .Bind().To<OwnOuter>()
            .Root<Own<OwnOuter>>("Root");
}
