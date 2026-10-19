/*
$v=true
$p=11
$d=Export attribute for a generic type
$sa=Export attribute
$h=The `[Export]` attribute works with generic types too: applied to a generic factory method with a `TT` marker, as in `[Export(typeof(IComments<TT>))]`, it makes a single method the source of `IComments<T>` for any requested `T`. This is handy when a factory class produces generic dependencies and you don't want to declare a separate binding for each closed type.
$f=>[!NOTE]
$f=>The Export attribute provides a declarative way to specify bindings directly on types, reducing the need for manual composition setup.
$f=
$f=The `TT` marker in the exported type is what makes one method serve every closed type: `IComments<Post>` and `IComments<Comment>` are both produced by the same factory call with the requested type argument, and the generated code is concrete for each of them.
$f=The exported method is called once per binding creation, so the lifetime of the owning type decides whether the result is reused. A factory that must return a fresh instance per consumer should stay transient, as it is here.
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