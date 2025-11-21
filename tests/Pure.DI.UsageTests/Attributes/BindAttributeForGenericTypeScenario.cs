/*
$v=true
$p=15
$d=Bind attribute for a generic type
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
namespace Pure.DI.UsageTests.Basics.BindAttributeForGenericTypeScenario;

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
    [Bind(typeof(IComments<TT>))]
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