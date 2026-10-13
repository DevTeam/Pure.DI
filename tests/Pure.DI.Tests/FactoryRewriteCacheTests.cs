namespace Pure.DI.Tests;

using Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class FactoryRewriteCacheTests
{
    private const string Code = """
                                class Dependency { }

                                class Factory
                                {
                                    object Create(int count)
                                    {
                                        Dependency  value = new Dependency();
                                        return count > 0 ? value : null;
                                    }
                                }
                                """;

    [Fact]
    public void ShouldProvideTypeOfName()
    {
        // Given
        var (semanticModel, root) = Compile();
        var name = FindName(root, "Dependency");
        var cache = new FactoryRewriteCache();

        // When
        var type = cache.GetTypeSymbol(semanticModel, name);

        // Then
        type.ShouldNotBeNull();
        type.Name.ShouldBe("Dependency");
        cache.GetTypeSymbol(semanticModel, name).ShouldBeSameAs(type);
    }

    [Fact]
    public void ShouldProvideNoTypeForNameOfValue()
    {
        // Given
        var (semanticModel, root) = Compile();
        var name = FindName(root, "count");
        var cache = new FactoryRewriteCache();

        // When
        var type = cache.GetTypeSymbol(semanticModel, name);

        // Then
        type.ShouldBeNull();
        cache.GetTypeSymbol(semanticModel, name).ShouldBeNull();
    }

    [Fact]
    public void ShouldReuseTypeSyntaxForSameNameAndType()
    {
        // Given
        var (_, root) = Compile();
        var name = FindName(root, "Dependency");
        var cache = new FactoryRewriteCache();

        // When
        var typeSyntax = cache.GetTypeSyntax(name, "global::Other");

        // Then
        typeSyntax.ToFullString().ShouldBe(name.ToFullString().Replace("Dependency", "global::Other"));
        cache.GetTypeSyntax(name, "global::Other").ShouldBeSameAs(typeSyntax);
    }

    [Fact]
    public void ShouldBuildTypeSyntaxForEachType()
    {
        // Given
        var (_, root) = Compile();
        var name = FindName(root, "Dependency");
        var cache = new FactoryRewriteCache();

        // When
        var first = cache.GetTypeSyntax(name, "global::First");
        var second = cache.GetTypeSyntax(name, "global::Second");

        // Then
        first.ToString().ShouldBe("global::First");
        second.ToString().ShouldBe("global::Second");
    }

    private static (SemanticModel SemanticModel, SyntaxNode Root) Compile()
    {
        var tree = CSharpSyntaxTree.ParseText(Code);
        var compilation = CSharpCompilation.Create(
            nameof(FactoryRewriteCacheTests),
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        return (compilation.GetSemanticModel(tree), tree.GetRoot());
    }

    // The first occurrence: for "Dependency", the type of the local variable.
    private static IdentifierNameSyntax FindName(SyntaxNode root, string name) =>
        root.DescendantNodes().OfType<IdentifierNameSyntax>().First(i => i.Identifier.Text == name);
}
