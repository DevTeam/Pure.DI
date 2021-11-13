namespace Pure.DI.Tests.Integration
{
    using Shouldly;
    using Xunit;

    public class FuncResolveTests
    {
        [Fact]
        public void ShouldSupportArray()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<int>>(2).To<Dep<int>>()
                            .Bind<IDep<int>[]>(1).To(ctx => new IDep<int>[]{ctx.Resolve<IDep<int>>(2), ctx.Resolve<IDep<int>>(2)})
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "2" }, generatedCode);
        }
        
        [Fact]
        public void ShouldSupportGenericArray()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<TT>>(2).To<Dep<TT>>()
                            .Bind<IDep<TT>[]>(1).To(ctx => new IDep<TT>[]{ctx.Resolve<IDep<TT>>(2), ctx.Resolve<IDep<TT>>(2)})
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "2" }, generatedCode);
        }
    }
}