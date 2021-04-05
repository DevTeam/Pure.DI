namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Text;

    internal class SourceBuilder : ISourceBuilder
    {
        private static readonly IConstructorsResolver ConstructorsResolver = new ConstructorsResolver();
        private static readonly IObjectBuilder ConstructorObjectBuilder = new ConstructorObjectBuilder(ConstructorsResolver);
        private static readonly IObjectBuilder FactoryObjectBuilder = new FactoryObjectBuilder();
        private static readonly IObjectBuilder ArrayObjectBuilder = new ArrayObjectBuilder();
        private static readonly IDefaultValueStrategy DefaultValueStrategy = new FallbackStrategy();
        private static readonly IResolverBuilder ResolverBuilder = new ResolverBuilder(DefaultValueStrategy);

        public IEnumerable<Source> Build(Compilation contextCompilation)
        {
            var assembly = GetType().Assembly;
            var features = new StringBuilder();
            var featuresCount = 0;
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.EndsWith("Feature.cs"))
                {
                    continue;
                }

                using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Cannot read {resourceName}."));
                features.AppendLine(reader.ReadToEnd());
                featuresCount++;
            }

            var compilation = CSharpCompilation
                .Create(contextCompilation.AssemblyName)
                .AddReferences(contextCompilation.References)
                .AddReferences(MetadataReference.CreateFromFile(typeof(DI).Assembly.Location))
                .WithOptions(contextCompilation.Options);

            compilation = (
                from tree in contextCompilation.SyntaxTrees
                let code = tree.ToString()
                where code.Contains($"{nameof(DI)}.{nameof(DI.Setup)}") 
                select tree)
                .Aggregate(
                    compilation,
                    (current, tree) 
                        => current.AddSyntaxTrees(CSharpSyntaxTree.ParseText(features + Environment.NewLine + tree)));

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var walker = new MetadataWalker(semanticModel);
                walker.Visit(tree.GetRoot());
                foreach (var rawMetadata in walker.Metadata.Skip(featuresCount))
                {
                    var metadata = CreateMetadata(rawMetadata, walker.Metadata);
                    var typeResolver = new TypeResolver(metadata, semanticModel, ConstructorObjectBuilder, FactoryObjectBuilder, ArrayObjectBuilder);
                    var compilationUnitSyntax = ResolverBuilder.Build(metadata, semanticModel, typeResolver);
                    yield return new Source(
                        metadata.TargetTypeName,
                        SourceText.From(compilationUnitSyntax.ToString(), Encoding.UTF8),
                        tree);
                }
            }
        }

        private static ResolverMetadata CreateMetadata(ResolverMetadata metadata, IReadOnlyCollection<ResolverMetadata> allMetadata)
        {
            var newMetadata = new ResolverMetadata(metadata.SetupNode, metadata.TargetTypeName);
            var dependencies = GetDependencies(metadata, new HashSet<ResolverMetadata>(), allMetadata);
            foreach (var dependency in dependencies)
            {
                foreach (var binding in dependency.Bindings)
                {
                    newMetadata.Bindings.Add(binding);
                }

                foreach (var fallback in dependency.Fallback)
                {
                    newMetadata.Fallback.Add(fallback);
                }
            }

            foreach (var binding in metadata.Bindings)
            {
                newMetadata.Bindings.Add(binding);
            }

            foreach (var fallback in metadata.Fallback)
            {
                newMetadata.Fallback.Add(fallback);
            }

            return newMetadata;
        }

        private static IEnumerable<ResolverMetadata> GetDependencies(ResolverMetadata metadata, ISet<ResolverMetadata> processed, IReadOnlyCollection<ResolverMetadata> allMetadata)
        {
            var dependencies =
                from dependencyName in metadata.DependsOn
                from dependency in allMetadata
                where dependencyName.Equals(dependency.TargetTypeName, StringComparison.InvariantCultureIgnoreCase)
                select dependency;

            foreach (var dependency in dependencies)
            {
                if (!processed.Add(dependency))
                {
                    continue;
                }

                yield return dependency;

                foreach (var nested in GetDependencies(dependency, processed, allMetadata))
                {
                    yield return nested;
                }
            }
        }
    }
}
