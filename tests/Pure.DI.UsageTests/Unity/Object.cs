// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Unity;

// ReSharper disable ConvertToStaticClass
// Unity resolves the fake through the type, so it stays a class that mirrors the Unity API shape.
public class Object
{
    public static T Instantiate<T>(T original)
    {
        return original;
    }
}