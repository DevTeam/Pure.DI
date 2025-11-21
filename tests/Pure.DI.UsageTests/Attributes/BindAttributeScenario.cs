/*
$v=true
$p=13
$d=Bind attribute
$h=`BindAttribute` allows you to perform automatic binding to properties, fields or methods that belong to the type of the binding involved.
$f=This attribute `BindAttribute` applies to field properties and methods, to regular, static, and even returning generalized types.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable LocalizableElement
namespace Pure.DI.UsageTests.Basics.BindAttributeScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
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
    // The [Bind] attribute specifies that the property is a source of dependency
    [Bind] public IGps Gps { get; } = new Gps();

    [Bind] public ICamera Camera { get; } = new Camera();
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