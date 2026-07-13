/*
$v=true
$p=3
$d=Partial constructor injection
$sa=Overload resolution priority
$sa=Constructor ordinal attribute
$h=C# 14 partial constructors let a type declare its construction contract in one part and provide the body in another. Pure.DI sees the combined constructor symbol, resolves its parameters once, and invokes it like a regular constructor. This is useful when a source generator owns the defining declaration while application code supplies the implementation.
$f=A partial constructor must have exactly one defining declaration ending with `;` and one implementing declaration with a body. Only the defining declaration participates in lookup, while constructor and parameter attributes from both parts are combined.
$f=Place `this(...)` or `base(...)` constructor initializers on the implementing declaration. `OrdinalAttribute`, `TagAttribute`, and `OverloadResolutionPriorityAttribute` can be placed on either part and are observed through the combined Roslyn symbol.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace Pure.DI.UsageTests.Attributes.PartialConstructorScenario;

using Shouldly;
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
            .Bind<IAuditTransport>("durable").To<FileAuditTransport>()
            .Bind().To(_ => new AuditSinkOptions(BatchSize: 128))

            // Composition root
            .Root<AuditSink>("AuditSink");

        var composition = new Composition();
        var sink = composition.AuditSink;

        sink.Transport.ShouldBeOfType<FileAuditTransport>();
        sink.BatchSize.ShouldBe(128);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IAuditTransport;

class FileAuditTransport : IAuditTransport;

record AuditSinkOptions(int BatchSize);

abstract class AuditSinkBase(IAuditTransport transport)
{
    public IAuditTransport Transport { get; } = transport;
}

partial class AuditSink : AuditSinkBase
{
    // This defining declaration participates in constructor lookup.
    // Its parameter attributes are merged with the implementing part.
    public partial AuditSink(
        [Tag("durable")] IAuditTransport transport,
        AuditSinkOptions options);

    public int BatchSize { get; private set; }
}

partial class AuditSink
{
    // A base/this initializer is allowed only on the implementing declaration.
    public partial AuditSink(
        IAuditTransport transport,
        AuditSinkOptions options)
        : base(transport)
    {
        BatchSize = options.BatchSize;
    }
}
// }
