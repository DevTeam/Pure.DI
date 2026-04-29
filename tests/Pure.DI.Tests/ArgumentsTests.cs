namespace Pure.DI.Tests;

using Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class ArgumentsTests
{
    [Fact]
    public void ShouldMapBaseArgumentsByPosition()
    {
        // Given
        var sut = new Arguments();
        var argumentList = ParseInvocationArgumentList("F(1, 2, 3);");

        // When
        var args = sut.GetArgs(argumentList, "x", "y", "z");

        // Then
        args[0]!.ToString().ShouldBe("1");
        args[1]!.ToString().ShouldBe("2");
        args[2]!.ToString().ShouldBe("3");
    }

    [Fact]
    public void ShouldMapBaseArgumentsByName()
    {
        // Given
        var sut = new Arguments();
        var argumentList = ParseInvocationArgumentList("F(y: 2, x: 1);");

        // When
        var args = sut.GetArgs(argumentList, "x", "y");

        // Then
        args[0]!.ToString().ShouldBe("x: 1");
        args[1]!.ToString().ShouldBe("y: 2");
    }

    [Fact]
    public void ShouldMapAttributeArgumentsInMixedMode()
    {
        // Given
        var sut = new Arguments();
        var argumentList = ParseAttributeArgumentList("[A(1, y: 2, 3)]");

        // When
        var args = sut.GetArgs(argumentList, "x", "y", "z");

        // Then
        args[0]!.ToString().ShouldBe("1");
        args[1]!.ToString().ShouldBe("y: 2");
        args[2]!.ToString().ShouldBe("3");
    }

    [Fact]
    public void ShouldMapAttributeArgumentsByNameInAnyOrder()
    {
        // Given
        var sut = new Arguments();
        var argumentList = ParseAttributeArgumentList("[A(namespaceName: \"Contracts\", asInternal: true, interfaceName: \"IMy\")]");

        // When
        var args = sut.GetArgs(argumentList, "namespaceName", "interfaceName", "asInternal");

        // Then
        args[0]!.ToString().ShouldBe("namespaceName: \"Contracts\"");
        args[1]!.ToString().ShouldBe("interfaceName: \"IMy\"");
        args[2]!.ToString().ShouldBe("asInternal: true");
    }

    [Fact]
    public void ShouldMapTypedConstantArgumentsWithNamedOverride()
    {
        // Given
        var sut = new Arguments();
        var attributeData = ParseAttributeData("""
            using System;
            [AttributeUsage(AttributeTargets.Class)]
            public sealed class AAttribute : Attribute
            {
                public AAttribute(string namespaceName, string interfaceName, bool asInternal) { }
            }
            [A("Demo", interfaceName: "IMy", asInternal: true)]
            public class C { }
            """);

        // When
        var args = sut.GetArgs(
            attributeData.ConstructorArguments,
            attributeData.NamedArguments,
            "namespaceName",
            "interfaceName",
            "asInternal");

        // Then
        args[0].Value.ShouldBe("Demo");
        args[1].Value.ShouldBe("IMy");
        args[2].Value.ShouldBe(true);
    }

    private static ArgumentListSyntax ParseInvocationArgumentList(string invocationStatement)
    {
        var tree = CSharpSyntaxTree.ParseText($$"""
            class C
            {
                void M()
                {
                    {{invocationStatement}}
                }
            }
            """);
        var root = tree.GetCompilationUnitRoot();
        return root.DescendantNodes().OfType<InvocationExpressionSyntax>().Single().ArgumentList;
    }

    private static AttributeArgumentListSyntax ParseAttributeArgumentList(string attributeText)
    {
        var tree = CSharpSyntaxTree.ParseText($$"""
            {{attributeText}}
            class C { }
            """);
        var root = tree.GetCompilationUnitRoot();
        return root.DescendantNodes().OfType<AttributeSyntax>().Single().ArgumentList!;
    }

    private static AttributeData ParseAttributeData(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "Tests",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semanticModel = compilation.GetSemanticModel(tree);
        var classSymbol = semanticModel.SyntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single(x => x.Identifier.Text == "C");
        return semanticModel.GetDeclaredSymbol(classSymbol)!
            .GetAttributes()
            .Single();
    }
}
