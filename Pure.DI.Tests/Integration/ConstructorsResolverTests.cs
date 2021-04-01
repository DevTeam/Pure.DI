namespace Pure.DI.Tests.Integration
{
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Microsoft.CodeAnalysis;
    using Shouldly;
    using Xunit;

    public class ConstructorsResolverTests
    {
        [Theory]
        [InlineData(@"namespace Sample { public class Cat { public Cat(string name) { } } }", "Sample.Cat", "Sample.Cat.Cat(string)")]
        [InlineData(@"namespace Sample { public class Cat { internal Cat(string name) { } } }", "Sample.Cat", "Sample.Cat.Cat(string)")]
        [InlineData(@"namespace Sample { public class Cat { private Cat(string name) { } } }", "Sample.Cat", null)]
        [InlineData(@"namespace Sample { }", "System.Random", "System.Random.Random(int)")]
        [InlineData(@"namespace Sample { public class Cat { [System.Obsolete] public Cat(string name) { } public Cat(int id) { } } }", "Sample.Cat", "Sample.Cat.Cat(int)")]
        public void ShouldProvideBindings(string code, string type, string expectedCtor)
        {
            // Given
            var (compilation, tree, root, semanticModel) = code.Compile();
            var ctorResolver = new ConstructorsResolver();
            
            // When
            var typeDescription = new TypeResolveDescription(
                new BindingMetadata(),
                compilation.GetTypeByMetadataName(type),
                null,
                null,
                new TypesMap(semanticModel),
                semanticModel,
                null);
            var actualCtor = ctorResolver.Resolve(typeDescription).FirstOrDefault();

            // Then
            actualCtor?.ToDisplayString().ShouldBe(expectedCtor);
        }
    }
}
