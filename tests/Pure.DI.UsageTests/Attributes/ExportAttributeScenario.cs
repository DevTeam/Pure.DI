/*
$v=true
$p=13
$d=Export attribute
$h=`ExportAttribute` lets you bind properties, fields, or methods declared on the bound type.
$f=It applies to instance or static members, including members that return generic types.
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
