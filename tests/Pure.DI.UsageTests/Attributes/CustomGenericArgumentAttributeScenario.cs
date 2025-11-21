/*
$v=true
$p=12
$d=Custom generic argument attribute
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
        // This hint indicates to not generate methods such as Resolve
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