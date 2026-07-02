/*
$v=true
$p=8
$d=Custom generic argument attribute
$sa=Custom generic argument
$h=Besides the built-in `TT` marker types, you can define your own generic type argument markers. Register a custom attribute with `GenericTypeArgumentAttribute<T>()`, apply it to a marker type like `TMy`, and use that marker in bindings: a single `Bind<IRepository<TMy>>().To<Repository<TMy>>()` then resolves `IRepository<T>` for any `T`, such as `IRepository<Post>` and `IRepository<Comment>`.
$f=>[!NOTE]
$f=>Custom generic argument attributes are useful when you need to pass metadata specific to generic type parameters during binding resolution.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedTypeParameter
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable InconsistentNaming

// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Attributes.CustomGenericArgumentAttributeScenario;

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
            // Registers custom generic argument
            .GenericTypeArgumentAttribute<GenericArgAttribute>()
            .Bind<IRepository<TMy>>().To<Repository<TMy>>()
            .Bind<IContentService>().To<ContentService>()

            // Composition root
            .Root<IContentService>("ContentService");

        var composition = new Composition();
        var service = composition.ContentService;
        service.Posts.ShouldBeOfType<Repository<Post>>();
        service.Comments.ShouldBeOfType<Repository<Comment>>();
// }
        composition.SaveClassDiagram();
    }
}

// {
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
class GenericArgAttribute : Attribute;

[GenericArg]
interface TMy;

interface IRepository<T>;

class Repository<T> : IRepository<T>;

class Post;

class Comment;

interface IContentService
{
    IRepository<Post> Posts { get; }

    IRepository<Comment> Comments { get; }
}

class ContentService(
    IRepository<Post> posts,
    IRepository<Comment> comments)
    : IContentService
{
    public IRepository<Post> Posts { get; } = posts;

    public IRepository<Comment> Comments { get; } = comments;
}
// }