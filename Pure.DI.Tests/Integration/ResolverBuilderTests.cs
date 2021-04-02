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
    using System;
    using Pure.DI;
    using static Pure.DI.Lifetime;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        private readonly IBox<ICat> _box;

        internal Program(IBox<ICat> box) => _box = box;        
    }

    internal static partial class Resolver
    {
        static Resolver()
        {
            DI.Setup()
                .Bind<Func<TT>>().To(ctx => new Func<TT>(ctx.Resolve<TT>))                
                // Represents a quantum superposition of 2 states: Alive or Dead
                .Bind<State>().To(ctx => (State)new Random().Next(2))
                // Represents schrodinger's cat
                .Bind<ICat>().To<ShroedingersCat>()
                // Represents a cardboard box with any content
                .Bind<IBox<TT>>().To<CardboardBox<TT>>()
                // Composition Root
                .Bind<Program>().As(Singleton).To<Program>();
        }
    }

    interface IBox<out T> { T Content { get; } }

    interface ICat { State State { get; } }

    enum State { Alive, Dead }

    class CardboardBox<T> : IBox<T>
    {
        public CardboardBox(T content) => Content = content;

        public T Content { get; }

        public override string ToString() => $""[{Content}]"";
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
}

",
            @"using System.Runtime.CompilerServices;

namespace Sample
{
    using System;
    using Pure.DI;
    using static Pure.DI.Lifetime;

    internal static partial class Resolver
    {
        private static readonly Context SharedContext = new Context();
        [MethodImplAttribute((MethodImplOptions)768)]
        public static T Resolve<T>()
        {
            if (typeof(Sample.State) == typeof(T))
            {
                return (T)(Object)((State)new Random().Next(2));
            }

            if (typeof(Sample.ICat) == typeof(T))
            {
                return (T)(Object)(new Sample.ShroedingersCat(new System.Lazy<Sample.State>((State)new Random().Next(2))));
            }

            if (typeof(Sample.Program) == typeof(T))
            {
                return (T)(Object)(ProgramSingleton.Shared);
            }

            return default(T);
        }

        [MethodImplAttribute((MethodImplOptions)768)]
        public static T Resolve<T>(Object tag)
        {
            return default(T);
        }

        [MethodImplAttribute((MethodImplOptions)768)]
        public static Object Resolve(Type type)
        {
            if (typeof(Sample.State) == type)
            {
                return ((State)new Random().Next(2));
            }

            if (typeof(Sample.ICat) == type)
            {
                return (new Sample.ShroedingersCat(new System.Lazy<Sample.State>((State)new Random().Next(2))));
            }

            if (typeof(Sample.Program) == type)
            {
                return (ProgramSingleton.Shared);
            }

            return default(Object);
        }

        [MethodImplAttribute((MethodImplOptions)768)]
        public static Object Resolve(Type type, Object tag)
        {
            return default(Object);
        }

        private static class ProgramSingleton
        {
            public static readonly Sample.Program Shared = new Sample.Program(new Sample.CardboardBox<Sample.ICat>(new Sample.ShroedingersCat(new System.Lazy<Sample.State>((State)new Random().Next(2)))));
        }

        private sealed class Context : IContext
        {
            [MethodImplAttribute((MethodImplOptions)768)]
            public T Resolve<T>()
            {
                return Resolver.Resolve<T>();
            }

            [MethodImplAttribute((MethodImplOptions)768)]
            public T Resolve<T>(Object tag)
            {
                return Resolver.Resolve<T>(tag);
            }
        }
    }
}")]

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