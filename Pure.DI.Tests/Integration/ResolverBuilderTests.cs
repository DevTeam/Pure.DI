namespace Pure.DI.Tests.Integration
{
    using System.Linq;
    using Core;
    using Shouldly;
    using Xunit;

    public class ResolverBuilderTests
    {
        [Theory]
        [InlineData(@"
namespace Sample
{ 
    using Pure.DI;

    public interface IBox<T> { T Content {get;} }

    public class Box<T> : IBox<T>
    {
        public Box(T content) => Content = content;

        public T Content { get; }
    }

    public interface ICat { }

    public class Cat { }

    public class Root
    {
        public Root(IBox<ICat> box) { }
    }

    public class Program
    {
        static void Main()
        {
            DI.Setup()
                .Bind<IBox<TT>>().To<Box<TT>>()
                .Bind<ICat>().To<Cat>()
                .Bind<Root>().To<Root>();
        }
    }    
}
",
            @"")]

        /*
        [InlineData(@"
namespace Sample
{ 
    using Pure.DI;

    public interface IName {}

    public class Name {}

    public class Cat
    {
        public Cat(IName name) { }
    }

    public class Program
    {
        static void Main()
        {
            DI.Setup()
                .Bind<IName>().To<Name>()
                .Bind<Cat>().To<Cat>();
        }
    }    
}
",
            @"")]

        */
        public void ShouldBuild(string code, string expectedCode)
        {
            // Given
            var (_, _, root, semanticModel) = code.Compile();
            var walker = new MetadataWalker(semanticModel);
            walker.Visit(root);
            var metadata = walker.Metadata.First();

            var builder = new ResolverBuilder(new ObjectBuilder(new ConstructorsResolver()));
            
            var typeResolver = new TypeResolver(metadata, semanticModel);

            // When
            var actualExpression = builder.Build(metadata, semanticModel, typeResolver).ToString();

            // Then
            actualExpression.ShouldBe(expectedCode);
        }
    }
}