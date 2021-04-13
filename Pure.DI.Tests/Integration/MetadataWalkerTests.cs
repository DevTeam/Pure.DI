// ReSharper disable StringLiteralTypo
namespace Pure.DI.Tests.Integration
{
    using System.Linq;
    using Core;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Shouldly;
    using Xunit;

    public class MetadataWalkerTests
    {
        private const string Code = @"
        namespace Sample
        {
            using Pure.DI;

            public class Program
            {
                public static void Main()
                {
                    DI.Setup(""My"")
                        .Bind<ICat>().As(Lifetime.Singleton).Tag(""Abc"").Tag(1).To<Cat>()
                        .Bind<ICat>().Bind<object>().As(Lifetime.Transient).To<Cat>();
                }        

                private void Abc()
                {
                    DI.Setup("""")
                        .Bind<ICat>().To<Cat>()
                        .Bind<string>().To<string>(ctx => ""Barsik"");
                }
            }

            public interface ICat { }
            public class Cat : ICat { }
        }
        ";

        [Fact]
        public void ShouldProvideBindings()
        {
            // Given
            var compilation = TestExtensions.CreateCompilation()
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(Code))
                .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
                .Check();

            var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
            var walker = new MetadataWalker(semanticModel);

            // When
            walker.Visit(compilation.SyntaxTrees.First().GetRoot());
            var metadata = walker.Metadata.ToList();

            // Then
            metadata.Count.ShouldBe(2);

            metadata[0].TargetTypeName.ShouldBe("My");
            var bindings = metadata[0].Bindings.ToList();
            bindings.Count.ShouldBe(2);
            bindings[0].Dependencies.Select(i => i.Type.ToDisplayString()).ToList().ShouldBe(new []{ "Sample.ICat" });
            bindings[0].Lifetime.ShouldBe(Lifetime.Singleton);
            var tags = bindings[0].Tags.ToList();
            tags.Count.ShouldBe(2);
            semanticModel.GetConstantValue(tags[0]).Value.ShouldBe("Abc");
            semanticModel.GetConstantValue(tags[1]).Value.ShouldBe(1);
            bindings[0].Implementation?.Type.ToDisplayString().ShouldBe("Sample.Cat");
            
            bindings[1].Dependencies.Select(i => i.Type.ToDisplayString()).ToList().ShouldBe(new[] { "Sample.ICat", "object" });
            bindings[1].Lifetime.ShouldBe(Lifetime.Transient);
            bindings[1].Tags.Count.ShouldBe(0);
            bindings[1].Implementation?.Type.ToDisplayString().ShouldBe("Sample.Cat");

            metadata[1].TargetTypeName.ShouldBe("");
            bindings = metadata[1].Bindings.ToList();
            bindings.Count.ShouldBe(2);
            bindings[0].Dependencies.Select(i => i.Type.ToDisplayString()).ToList().ShouldBe(new[] { "Sample.ICat" });
            bindings[0].Lifetime.ShouldBe(Lifetime.Transient);
            bindings[0].Tags.Count.ShouldBe(0);
            bindings[0].Implementation?.Type.ToDisplayString().ShouldBe("Sample.Cat");

            bindings[1].Dependencies.Select(i => i.Type.ToDisplayString()).ToList().ShouldBe(new[] { "string" });
            bindings[1].Lifetime.ShouldBe(Lifetime.Transient);
            bindings[1].Tags.Count.ShouldBe(0);
            bindings[1].Implementation?.Type.ToDisplayString().ShouldBe("string");
            bindings[1].Factory?.ToString().ShouldBe(@"ctx => ""Barsik""");
        }
    }
}
