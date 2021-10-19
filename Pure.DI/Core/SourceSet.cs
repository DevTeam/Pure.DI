namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Text;

    internal class SourceSet
    {
        private static readonly List<(string name, string text)> Features;
        private static readonly List<(string name, string text)> Components;
        public readonly IReadOnlyList<Source> FeatureSources;
        public readonly IReadOnlyList<Source> ComponentSources;

        static SourceSet()
        {
            Regex featuresRegex = new(@"Pure.DI.Features.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Regex componentsRegex = new(@"Pure.DI.Components.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Features = new List<(string, string)> { new("Features.cs", string.Join(Environment.NewLine, GetResources(featuresRegex).Select(i => i.code))) };
            Components = GetResources(componentsRegex).Select(i => (i.file, i.code)).ToList();
        }

        public SourceSet(CSharpParseOptions parseOptions, string ns)
        {
            ComponentSources = CreateSources(Components, parseOptions, ns).ToList();
            FeatureSources = CreateSources(Features, parseOptions).ToList();
        }

        private static IEnumerable<Source> CreateSources(IEnumerable<(string name, string text)> sources, CSharpParseOptions parseOptions, string ns = "") =>
            from source in sources 
            let text = string.IsNullOrWhiteSpace(ns) ? source.text : source.text.Replace("Pure.DI", $"Pure.DI.{ns}") 
            //let text = source.text
            select new Source(source.name, SourceText.From(text, Encoding.UTF8), CSharpSyntaxTree.ParseText(text, parseOptions));
        
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
                var code = reader.ReadToEnd();
                yield return (resourceName, code);
            }
        }
    }
}