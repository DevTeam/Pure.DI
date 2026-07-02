/*
$v=true
$p=15
$d=Export attribute for a generic type
$h=The `[Export]` attribute works with generic types too: applied to a generic factory method with a `TT` marker, as in `[Export(typeof(IComments<TT>))]`, it makes a single method the source of `IComments<T>` for any requested `T`. This is handy when a factory class produces generic dependencies and you don't want to declare a separate binding for each closed type.
$f=>[!NOTE]
$f=>The Export attribute provides a declarative way to specify bindings directly on types, reducing the need for manual composition setup.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1822
namespace Pure.DI.UsageTests.Basics.ExportAttributeForGenericTypeScenario;

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
            .Bind().As(Lifetime.Singleton).To<CommentsFactory>()
            .Bind().To<ArticleService>()

            // Composition root
            .Root<IArticleService>("ArticleService");

        var composition = new Composition();
        var articleService = composition.ArticleService;
        articleService.DisplayComments();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IComments<T>
{
    void Load();
}

class Comments<T> : IComments<T>
{
    public void Load()
    {
    }
}

class CommentsFactory
{
    // The 'TT' type marker in the attribute indicates that this method
    // can produce 'IComments<T>' for any generic type 'T'.
    // This allows the factory to handle all requests for IComments<T>.
    [Export(typeof(IComments<TT>))]
    public IComments<T> Create<T>() => new Comments<T>();
}

interface IArticleService
{
    void DisplayComments();
}

class ArticleService(IComments<Article> comments) : IArticleService
{
    public void DisplayComments() => comments.Load();
}

class Article;
// }

#pragma warning restore CA1822