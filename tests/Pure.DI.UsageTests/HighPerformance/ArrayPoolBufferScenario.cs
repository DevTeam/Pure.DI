/*
$v=true
$p=HighPerformance:4
$d=ArrayPool buffer
$sa=Span and ReadOnlySpan
$sa=Tracking disposable instances per a composition root
$h=Use `ArrayPool<T>` when a hot path needs temporary buffers whose size is too large or too variable for `stackalloc`. Pure.DI supports `ArrayPool<T>` out of the box, so the setup does not need to bind `ArrayPool<byte>.Shared` manually; request `ArrayPool<byte>` like any other dependency.
$h=In this example a CSV export endpoint creates a per-request `ExportBuffer`. The buffer owner receives the shared `ArrayPool<byte>` from Pure.DI, rents an array using application options, exposes only `Memory<byte>` to the exporter, and returns the array when the `Owned<IReportExporter>` operation scope is disposed.
$f=This pattern is useful for serialization, compression, protocol framing, and batch export pipelines. Keep the owner lifetime short, clear sensitive buffers before returning them, and do not store the rented `Memory<T>` after the owner is disposed.
$f=The important DI detail is ownership: bind or auto-bind the owner as the dependency that is tracked and disposed, not the raw array. The pool itself is a built-in BCL dependency; the owner is the application-specific object that defines rent size, cleanup, and return rules. `Owned<T>` keeps that cleanup tied to one root call instead of the whole composition.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable InvertIf
namespace Pure.DI.UsageTests.HighPerformance.ArrayPoolBufferScenario;

using System.Buffers;
using System.Text;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Buffers;
//# using System.Text;
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
            .Bind().To(_ => new ExportBufferOptions(Size: 256))
            .Bind().To<ExportBuffer>()
            .Bind<IReportExporter>().To<CsvReportExporter>()
            .Root<Owned<IReportExporter>>("Exporter");

        var composition = new Composition();
        var exporter = composition.Exporter;

        var bytesWritten = exporter.Value.Export(
            [
                new Order(17, 42.50m),
                new Order(18, 13.25m)
            ]);

        bytesWritten.ShouldBeGreaterThan(0);
        exporter.Value.Buffer.Returned.ShouldBeFalse();
// }
        exporter.Dispose();
        exporter.Value.Buffer.Returned.ShouldBeTrue();
        composition.SaveClassDiagram();
    }
}

// {
readonly record struct Order(int Id, decimal Total);

readonly record struct ExportBufferOptions(int Size);

interface IReportExporter
{
    ExportBuffer Buffer { get; }

    int Export(IReadOnlyList<Order> orders);
}

sealed class CsvReportExporter(ExportBuffer buffer) : IReportExporter
{
    public ExportBuffer Buffer => buffer;

    public int Export(IReadOnlyList<Order> orders)
    {
        var writer = new ArrayBufferWriter<byte>(buffer.Memory.Length);
        foreach (var order in orders)
        {
            var line = $"order-{order.Id},{order.Total:0.00}\n";
            writer.Write(Encoding.UTF8.GetBytes(line));
        }

        writer.WrittenSpan.CopyTo(buffer.Memory.Span);
        return writer.WrittenCount;
    }
}

sealed class ExportBuffer(
    ArrayPool<byte> pool,
    ExportBufferOptions options)
    : IDisposable
{
    private byte[]? _buffer = pool.Rent(options.Size);

    public Memory<byte> Memory => _buffer;

    public bool Returned => _buffer is null;

    public void Dispose()
    {
        if (_buffer is {} buffer)
        {
            Array.Clear(buffer);
            pool.Return(buffer);
            _buffer = null;
        }
    }
}
// }
