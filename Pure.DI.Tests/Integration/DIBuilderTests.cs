namespace Pure.DI.Tests.Integration
{
    using System.Collections.Generic;
    using Core;
    using Microsoft.CodeAnalysis.CSharp;
    using Shouldly;
    using Xunit;

    public class DIBuilderTests
    {
        [Theory]
        [InlineData(@"
namespace Sample
{ 
    public interface IName {}

    public class Name {}

    public class Cat
    {
        public Cat(IName name) { }
    } 
}
",
            @"namespace Abc
{
    internal static class Composition
    {
        public static T Resolve<T>()
        {
            if (typeof(Sample.Cat) == typeof(T))
            {
                return new Sample.Cat(new Sample.Name());
            }

            if (typeof(Sample.IName) == typeof(T))
            {
                return new Sample.Name();
            }
        }
    }
}")]


        public void ShouldBuild(string code, string expectedExpression)
        {
            // Given
            var (compilation, tree, root, semanticModel) = code.Compile();
            var builder =  new DIBuilder(new ObjectBuilder(new ConstructorsResolver()));
            var metadata = new DIMetadata(tree, "Samples", "Composition", new List<BindingMetadata>()
            {
                new()
                {
                    ImplementationType = compilation.GetTypeByMetadataName("Sample.Cat"),
                    ContractTypes = { compilation.GetTypeByMetadataName("Sample.Cat") }
                },
                new()
                {
                    ImplementationType = compilation.GetTypeByMetadataName("Sample.Name"),
                    ContractTypes = { compilation.GetTypeByMetadataName("Sample.IName") }
                }
            });

            var typeResolver = new TypeResolver(metadata);

            // When
            var actualExpression = builder.Build(metadata, semanticModel, typeResolver).ToString();

            // Then
            actualExpression.ShouldBe(expectedExpression);
        }

        [Theory]
        [InlineData(@"
namespace Sample
{ 
    public interface IName {}

    public class Name {}

    public class Cat
    {
        public Cat(IName name) { }
    } 
}
",
            @"namespace Abc
{
    internal static class Composition
    {
        public static T Resolve<T>()
        {
            if (typeof(Sample.IName) == typeof(T))
            {
                return new Sample.Name();
            }
        }

        public static T Resolve<T>(object tag)
        {
            if (typeof(Sample.Cat) == typeof(T))
            {
                if (33.Equals(tag))
                {
                    return new Sample.Cat(new Sample.Name());
                }
            }
        }
    }
}")]

        public void ShouldBuildWhenTag(string code, string expectedExpression)
        {
            // Given
            var (compilation, tree, root, semanticModel) = code.Compile();
            var builder = new DIBuilder(new ObjectBuilder(new ConstructorsResolver()));
            var metadata = new DIMetadata(tree,"Samples", "Composition", new List<BindingMetadata>()
            {
                new()
                {
                    ImplementationType = compilation.GetTypeByMetadataName("Sample.Cat"),
                    ContractTypes = { compilation.GetTypeByMetadataName("Sample.Cat") },
                    Tags = { SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression).WithToken(SyntaxFactory.Literal(33)) }
                },
                new()
                {
                    ImplementationType = compilation.GetTypeByMetadataName("Sample.Name"),
                    ContractTypes = { compilation.GetTypeByMetadataName("Sample.IName") }
                }
            });

            var typeResolver = new TypeResolver(metadata);

            // When
            var actualExpression = builder.Build(metadata, semanticModel, typeResolver).ToString();

            // Then
            actualExpression.ShouldBe(expectedExpression);
        }
    }
}