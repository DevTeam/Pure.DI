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
        private static readonly Regex FeaturesRegex = new(@"Pure.DI.Features.[\w]+Feature.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly string Features;
        private readonly IBuildContext _context;
        private readonly IDiagnostic _diagnostic;
        private readonly Func<IResolverBuilder> _resolverBuilderFactory;
        private readonly Func<SemanticModel, IMetadataWalker> _metadataWalkerFactory;

        static SourceBuilder()
        {
            var assembly = typeof(SourceGenerator).Assembly;
            var sb = new StringBuilder();
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!FeaturesRegex.IsMatch(resourceName))
                {
                    continue;
                }

                using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Cannot read {resourceName}."));
                sb.AppendLine(reader.ReadToEnd());
            }

            Features = sb.ToString();
        }

        public SourceBuilder(
            IBuildContext context,
            IDiagnostic diagnostic,
            Func<IResolverBuilder> resolverBuilderFactory,
            Func<SemanticModel, IMetadataWalker> metadataWalkerFactory)
        {
            _context = context;
            _diagnostic = diagnostic;
            _resolverBuilderFactory = resolverBuilderFactory;
            _metadataWalkerFactory = metadataWalkerFactory;
        }

        public IEnumerable<Source> Build(Compilation compilation)
        {
            if (!(compilation is CSharpCompilation csharpCompilation))
            {
                _diagnostic.Error(Diagnostics.Unsupported, $"{compilation.Language} is not supported.");
                throw Diagnostics.ErrorShouldTrowException;
            }

            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(Features, new CSharpParseOptions(csharpCompilation.LanguageVersion)));
            var featuresMetadata = new List<ResolverMetadata>();
            foreach (var tree in compilation.SyntaxTrees.Reverse().Take(1))
            {
                var walker = _metadataWalkerFactory(compilation.GetSemanticModel(tree));
                walker.Visit(tree.GetRoot());
                featuresMetadata.AddRange(walker.Metadata);
            }

            foreach (var tree in compilation.SyntaxTrees.Reverse().Skip(1))
            {
                var walker = _metadataWalkerFactory(compilation.GetSemanticModel(tree));
                walker.Visit(tree.GetRoot());
                foreach (var rawMetadata in walker.Metadata)
                {
                    var allMetadata = new List<ResolverMetadata>(featuresMetadata) { rawMetadata };
                    var metadata = CreateMetadata(rawMetadata, allMetadata);
                    _context.Prepare(metadata);
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
