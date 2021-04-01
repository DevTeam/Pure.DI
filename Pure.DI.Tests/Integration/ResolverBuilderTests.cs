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
    using System;

interface IBox<out T> { T Content { get; } }

        interface ICat { State State { get; } }

        enum State { Alive, Dead }

        class CardboardBox<T> : IBox<T>
        {
            public CardboardBox(T content) => Content = content;

            public T Content { get; }
        }

        class ShroedingersCat : ICat
        {
            // Represents the superposition of the states
            private readonly Lazy<State> _superposition;

            public ShroedingersCat(Lazy<State> superposition) => _superposition = superposition;

            // The decoherence of the superposition at the time of observation via an irreversible process
            public State State => _superposition.Value;

            public override string ToString() => $""{State} cat"";
    }

public class Program
    {
        static void Main()
        {
            DI.Setup()
                .Bind<Func<TT>>().To(ctx => new Func<TT>(() => ctx.Resolve<TT>()))
                .Bind<Lazy<TT>>().To(ctx => new Lazy<TT>(ctx.Resolve<Func<TT>>(), true))
                // Represents a quantum superposition of 2 states: Alive or Dead
                .Bind<State>().To(ctx => (State)new Random().Next(2))
                // Represents schrodinger's cat
                .Bind<ICat>().To<ShroedingersCat>()
                // Represents a cardboard box with any content
                .Bind<IBox<TT>>().To<CardboardBox<TT>>();
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