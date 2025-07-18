#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Pure.DI.UsageTests.Builders;

using Pure.DI;

public class BuildersTest
{
    [Fact]
    public void Test()
    {
        var composition = new Composition();

        var main = composition.Main;
        if(main.Scene.Value.Dependency is null)
        {
            throw new Exception("Dependency is null");
        }
    }
}

public partial class Composition
{
    // [Conditional("DI")]
    static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().As(Lifetime.Singleton).To<Dependency>()
            .GenericTypeArgument<TTNode>()
            .Bind<Accessor<TTNode>>(Tag.Any)
                .As(Lifetime.PerResolve)
                .To(ctx =>
                {
                    var node = Loader.Load<TTNode>();
                    ctx.BuildUp(node);
                    return new Accessor<TTNode>(node);
                })
            .Root<Main>("Main");

    class TTNode : Node;
}

public class Node;

public class Dependency;

public class Scene : Node
{
    [Dependency]
    public Dependency Dependency { get; set; }
}

public class Main
{
    [Dependency]
    public Accessor<Scene> Scene { get; set; }
}

public static class Loader
{
    public static T Load<T>() => Activator.CreateInstance<T>();
}

public struct Accessor<T>
    where T : Node
{
    public Accessor(T value)
    {
        Value = value;
    }

    public T Value { get; }
}