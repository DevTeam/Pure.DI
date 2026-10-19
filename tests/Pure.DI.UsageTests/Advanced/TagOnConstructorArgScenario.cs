/*
$v=true
$p=17
$d=Tag on a constructor argument
$sa=Tag on injection site
$sa=Tag on a member
$sa=Tag on a method argument
$h=`Tag.OnConstructorArg<T>(name)` creates a tag that targets the constructor parameter `name` of type `T`, letting you choose a specific implementation for a single injection site without adding `[Tag(...)]` attributes to the consuming class — useful when you can't or don't want to modify its code. The wildcards `*` and `?` are supported.
$f=Reach for it when a type from another library, or a type you do not own, needs a different implementation for one of its dependencies - a file stream for the source and a network stream for the target in this example.
$f=The tag is matched against the constructor parameter name and the declaring type, so renaming the parameter or moving it to another type silently turns the binding into an unused one: `DIW004` reports that the injection site was never matched.
$f=Generic consumer types work the same way: `Tag.OnConstructorArg<StreamProcessor<TT>>("stream")` applies to every closed instantiation of the processor.
$f=>[!WARNING]
$f=>Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.
$f=
$f=Prefer `[Tag("...")]` on the parameter when you own the type, because the compiler then checks the tag next to the code it affects.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.TagOnConstructorArgScenario;

using Pure.DI;
using UsageTests;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind(Tag.OnConstructorArg<DataReplicator>("sourceStream"))
                .To<FileStream>()
            .Bind(Tag.OnConstructorArg<StreamProcessor<TT>>("stream"))
                .To<NetworkStream>()
            .Bind<IDataReplicator>().To<DataReplicator>()

            // Specifies to create the composition root named "Root"
            .Root<IDataReplicator>("Replicator");

        var composition = new Composition();
        var replicator = composition.Replicator;
        replicator.SourceStream.ShouldBeOfType<FileStream>();
        replicator.TargetStream.ShouldBeOfType<NetworkStream>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IStream;

class FileStream : IStream;

class NetworkStream : IStream;

class StreamProcessor<T>(IStream stream)
{
    public IStream Stream { get; } = stream;
}

interface IDataReplicator
{
    IStream SourceStream { get; }

    IStream TargetStream { get; }
}

class DataReplicator(
    IStream sourceStream,
    StreamProcessor<string> processor)
    : IDataReplicator
{
    public IStream SourceStream { get; } = sourceStream;

    public IStream TargetStream => processor.Stream;
}
// }