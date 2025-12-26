namespace Pure.DI.UsageTests.Unity;

public class Object
{
    public static T Instantiate<T>(T original)
    {
        return original;
    }
}