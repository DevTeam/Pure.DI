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
        private static readonly Regex FeaturesRegex = new(@"Pure.DI.Features.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex ComponentsRegex = new(@"Pure.DI.Components.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Source Features;
        private static readonly IEnumerable<Source> Components;
        private readonly IBuildContext _context;
        private readonly IDiagnostic _diagnostic;
        private readonly Func<IClassBuilder> _resolverBuilderFactory;
        private readonly Func<SemanticModel, IMetadataWalker> _metadataWalkerFactory;

        static SourceBuilder()
        {
            Features = new Source("Features.cs",SourceText.From(string.Join(Environment.NewLine, GetResources(FeaturesRegex)), Encoding.UTF8));
            Components = GetResources(ComponentsRegex).Select(i => new Source(i.file, SourceText.From(i.code, Encoding.UTF8))).ToArray();
        }

        private static IEnumerable<(string file, string code)> GetResources(Regex filter)
        {
            var assembly = typeof(SourceGenerator).Assembly;

            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!filter.IsMatch(resourceName))
                {
                    continue;
                }

                using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Cannot read {resourceName}."));
                yield return (resourceName, reader.ReadToEnd());
            }
        }

        public SourceBuilder(
            IBuildContext context,
            IDiagnostic diagnostic,
            Func<IClassBuilder> resolverBuilderFactory,
            Func<SemanticModel, IMetadataWalker> metadataWalkerFactory)
        {
            _context = context;
            _diagnostic = diagnostic;
            _resolverBuilderFactory = resolverBuilderFactory;
            _metadataWalkerFactory = metadataWalkerFactory;
        }

        public IEnumerable<Source> Build(Compilation compilation)
        {
            if (compilation is not CSharpCompilation csharpCompilation)
            {
                _diagnostic.Error(Diagnostics.Unsupported, $"{compilation.Language} is not supported.");
                throw Diagnostics.ErrorShouldTrowException;
            }

            var parseOptions = new CSharpParseOptions(csharpCompilation.LanguageVersion);
            foreach (var component in Components)
            {
                compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(component.Code, parseOptions));
                yield return component;
            }

            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(Features.Code, parseOptions));

            var featuresMetadata = new List<ResolverMetadata>();
            foreach (var tree in compilation.SyntaxTrees.Reverse().Take(1))
            {
                var walker = _metadataWalkerFactory(compilation.GetSemanticModel(tree));
                walker.Visit(tree.GetRoot());
                featuresMetadata.AddRange(walker.Metadata);
            }

            foreach (var tree in compilation.SyntaxTrees.Reverse().Skip(1 + Components.Count()))
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var walker = _metadataWalkerFactory(compilation.GetSemanticModel(tree));
                walker.Visit(tree.GetRoot());
                foreach (var rawMetadata in walker.Metadata)
                {
                    var allMetadata = new List<ResolverMetadata>(featuresMetadata) { rawMetadata };
                    var metadata = CreateMetadata(rawMetadata, allMetadata);
                    _context.Prepare(metadata);
                    var compilationUnitSyntax = _resolverBuilderFactory().Build(semanticModel);
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

                foreach (var attribute in dependency.Attributes)
                {
                    newMetadata.Attributes.Add(attribute);
                }
            }

            foreach (var binding in metadata.Bindings)
            {
                newMetadata.Bindings.Add(binding);
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
