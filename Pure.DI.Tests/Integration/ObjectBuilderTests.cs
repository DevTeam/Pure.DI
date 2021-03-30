namespace Pure.DI.Tests.Integration
{
    using Core;
    using Microsoft.CodeAnalysis;
    using Moq;
    using Shouldly;
    using Xunit;

    public class ObjectBuilderTests
    {
        [Theory]
        [InlineData(@"
namespace Sample
{ 
    public interface IName {}

    public class Name {}
}
",
            "Sample.Name",
            "new Sample.Name()")]

        [InlineData(@"
namespace Sample
{ 
    public interface IName {}

    public class Name {}
}
",
            "Sample.IName",
            "new Sample.Name()")]

        [InlineData(@"
namespace Sample
{ 
    public interface IName {}

    public class Name {}

    public class Cat
    {
        public Cat(Name name) { }
    } 
}
", 
            "Sample.Cat",
            "new Sample.Cat(new Sample.Name())")]

        [InlineData(@"
namespace Sample
{ 
    public interface IName {}

    public class Name: IName {}

    public class Cat
    {
        public Cat(IName name) { }
    } 
}
",
            "Sample.Cat",
            "new Sample.Cat(new Sample.Name())")]

        [InlineData(@"
namespace Sample
{ 
    public interface ICatName {}
    
    public class Cat
    {
        public Cat(ICatName name) { }
    } 
}
",
            "Sample.Cat",
            null)]

        public void ShouldBuild(string code, string type, string expectedExpression)
        {
            // Given
            var (compilation, tree, root, semanticModel) = code.Compile();
            var builder =  new ObjectBuilder(new ConstructorsResolver());
            var typeResolver = new Mock<ITypeResolver>();
            var targetType = compilation.GetTypeByMetadataName(type);

            typeResolver.Setup(i => i.Resolve(It.IsAny<INamedTypeSymbol>())).Returns<INamedTypeSymbol>(i => i);
            
            var contractType = compilation.GetTypeByMetadataName("Sample.IName");
            var implementationType = compilation.GetTypeByMetadataName("Sample.Name");
            typeResolver.Setup(i => i.Resolve(It.Is<INamedTypeSymbol>(j => SymbolEqualityComparer.IncludeNullability.Equals(j, contractType)))).Returns(implementationType);

            // When
            var actualExpression = builder.TryBuild(targetType, semanticModel, typeResolver.Object)?.ToString();

            // Then
            actualExpression.ShouldBe(expectedExpression);
        }

        [Theory]
        [InlineData(@"
namespace Sample
{ 
    public interface IName<T> {}

    public class Name<T> {}

    public class Name {}

    public class Cat<T>
    {
        public Cat(Name name) { }
    } 
}
", 
            "new Sample.Cat<string>(new Sample.Name())")]

        [InlineData(@"
namespace Sample
{ 
    public interface IName<T> {}

    public class Name<T>: IName<T> {}

    public class Cat<T>
    {
        public Cat(IName<T> name) { }
    } 
}
", 
            "new Sample.Cat<string>(new Sample.Name<string>())")]

        public void ShouldBuildWhenSimpleGeneric(string code, string expectedExpression)
        {
            // Given
            var (compilation, tree, root, semanticModel) = code.Compile();
            var builder = new ObjectBuilder(new ConstructorsResolver());
            var typeResolver = new Mock<ITypeResolver>();
            var targetType = compilation.GetTypeByMetadataName("Sample.Cat`1").Construct(compilation.GetSpecialType(SpecialType.System_String));

            typeResolver.Setup(i => i.Resolve(It.IsAny<INamedTypeSymbol>())).Returns<INamedTypeSymbol>(i => i);

            var contractType = compilation.GetTypeByMetadataName("Sample.IName`1").Construct(compilation.GetSpecialType(SpecialType.System_String));
            var implementationType = compilation.GetTypeByMetadataName("Sample.Name`1").Construct(compilation.GetSpecialType(SpecialType.System_String));
            typeResolver.Setup(i => i.Resolve(It.Is<INamedTypeSymbol>(j => SymbolEqualityComparer.IncludeNullability.Equals(j, contractType)))).Returns(implementationType);

            // When
            var actualExpression = builder.TryBuild(targetType, semanticModel, typeResolver.Object)?.ToString();

            // Then
            actualExpression.ShouldBe(expectedExpression);
        }
    }
}