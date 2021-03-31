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

    public class Cat
    {
        public Cat(string name) { }
    }

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
                .Bind<string>().Tag(1).To<string>(ctx => ""Barsik"")
                .Bind<string>().To<string>(ctx => { return ""Cat"" + ctx.Resolve<string>(1); })
                .Bind<ICat>().To<Cat>()
                .Bind<Root>().As(Lifetime.Singleton).Tag(10).To<Root>()
                .Bind<Root>().As(Lifetime.Transient).To<Root>();
        }
    }    
}
",
            @"")]

        public void ShouldBuild(string code, string expectedCode)
        {
            // Given
            var (_, _, root, semanticModel) = code.Compile();
            var walker = new MetadataWalker(semanticModel);
            walker.Visit(root);
            var metadata = walker.Metadata.First();

            var builder = new ResolverBuilder();
            var constructorObjectBuilder = new ConstructorObjectBuilder(new ConstructorsResolver());
            var factoryObjectBuilder = new FactoryObjectBuilder();
            var typeResolver = new TypeResolver(metadata, semanticModel, constructorObjectBuilder, factoryObjectBuilder);

            // When
            var actualExpression = builder.Build(metadata, semanticModel, typeResolver).ToString();

            // Then
            actualExpression.ShouldBe(expectedCode);
        }
    }
}