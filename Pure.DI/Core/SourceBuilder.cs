namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Text;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SourceBuilder : ISourceBuilder
    {
        private readonly IBuildContext _context;
        private readonly Func<IResolverBuilder> _resolverBuilderFactory;
        private readonly Func<IMetadataWalker> _metadataWalkerFactory;
        private static readonly Regex FeaturesRegex = new(@"Pure.DI.Features.[\w]+Feature.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public SourceBuilder(
            IBuildContext context,
            Func<IResolverBuilder> resolverBuilderFactory,
            Func<IMetadataWalker> metadataWalkerFactory)
        {
            _context = context;
            _resolverBuilderFactory = resolverBuilderFactory;
            _metadataWalkerFactory = metadataWalkerFactory;
        }

        public IEnumerable<Source> Build(Compilation contextCompilation, IEnumerable<SyntaxTree> treesWithMetadata)
        {
            var assembly = GetType().Assembly;
            var features = new StringBuilder();
            var featuresCount = 0;
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!FeaturesRegex.IsMatch(resourceName))
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
                .WithOptions(contextCompilation.Options);

            foreach (var tree in treesWithMetadata)
            {
                compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(features + Environment.NewLine + tree));
            }

            foreach (var tree in compilation.SyntaxTrees)
            {
                _context.SemanticModel = compilation.GetSemanticModel(tree);
                var walker = _metadataWalkerFactory();
                walker.Visit(tree.GetRoot());
                foreach (var rawMetadata in walker.Metadata.Skip(featuresCount))
                {
                    var metadata = CreateMetadata(rawMetadata, walker.Metadata);
                    _context.Metadata = metadata;
                    var compilationUnitSyntax = _resolverBuilderFactory().Build();
                    yield return new Source(
                        metadata.TargetTypeName,
                        SourceText.From(compilationUnitSyntax.ToString(), Encoding.UTF8));
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

                foreach (var attribute in dependency.Attributes)
                {
                    newMetadata.Attributes.Add(attribute);
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

            foreach (var attribute in metadata.Attributes)
            {
                newMetadata.Attributes.Add(attribute);
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
