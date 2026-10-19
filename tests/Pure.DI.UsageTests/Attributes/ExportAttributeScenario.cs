/*
$v=true
$p=9
$d=Export attribute
$sa=Export attribute with lifetime and tag
$sa=Export attribute for a generic type
$h=`ExportAttribute` lets you bind properties, fields, or methods declared on the bound type. The member becomes the source of a dependency, so a subsystem that already owns its collaborators can publish them to the graph instead of having the composition rebuild them.
$f=It applies to instance or static members, including members that return generic types.
$f=This is how a class that constructs its own internals once — a device provider, a plugin, a configuration holder — hands them over without exposing setters. `PhotoService` receives the exported `IGps` and a `Func<ICamera>`, the latter built from the same exported property.
$f=An exported member is only read when the binding is created: with a `Singleton` owner the exported value is captured once, with a transient owner it is read again for every instance. The member type has to be resolvable as a contract in the graph.
$f=>[!NOTE]
$f=>Combining `Export` with lifetime and tags, as in [export attribute with lifetime and tag](export-attribute-with-lifetime-and-tag.md), is the usual way to publish one instance to many consumers.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable LocalizableElement
namespace Pure.DI.UsageTests.Basics.ExportAttributeScenario;

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
            .Bind().As(Lifetime.Singleton).To<DeviceFeatureProvider>()
            .Bind().To<PhotoService>()

            // Composition root
            .Root<IPhotoService>("PhotoService");

        var composition = new Composition();
        var photoService = composition.PhotoService;
        photoService.TakePhotoWithLocation();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IGps
{
    void GetLocation();
}

class Gps : IGps
{
    public void GetLocation() => Console.WriteLine("Coordinates: 123, 456");
}

interface ICamera
{
    void Capture();
}

class Camera : ICamera
{
    public void Capture() => Console.WriteLine("Photo captured");
}

class DeviceFeatureProvider
{
    // The [Export] attribute specifies that the property is a source of dependency
    [Export] public IGps Gps { get; } = new Gps();

    [Export] public ICamera Camera { get; } = new Camera();
}

interface IPhotoService
{
    void TakePhotoWithLocation();
}

class PhotoService(IGps gps, Func<ICamera> cameraFactory) : IPhotoService
{
    public void TakePhotoWithLocation()
    {
        gps.GetLocation();
        cameraFactory().Capture();
    }
}
// }
