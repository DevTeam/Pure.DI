namespace Pure.DI.Core;

using System.IO;
using System.Text.RegularExpressions;

internal class SourceSet
{
    private static readonly Regex FeaturesRegex = new(@"Pure.DI.Features.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex ComponentsRegex = new(@"Pure.DI.Components.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    public readonly IReadOnlyList<Source> ApiSources;
    public readonly IEnumerable<SyntaxTree> FeaturesTrees;
    public readonly IEnumerable<SyntaxTree> ApiTrees;

    public SourceSet(CSharpParseOptions parseOptions)
    {
        ApiSources = GetResources(ComponentsRegex).ToList();
        FeaturesTrees = GetResources(FeaturesRegex).Select(source => CSharpSyntaxTree.ParseText(source.Code, parseOptions)).ToList();
        ApiTrees = ApiSources.Select(source => CSharpSyntaxTree.ParseText(source.Code, parseOptions)).ToList();
    }

    private static IEnumerable<Source> GetResources(Regex filter)
    {
        var assembly = typeof(SourceSet).Assembly;
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!filter.IsMatch(resourceName))
            {
                continue;
            }

            using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Cannot read {resourceName}."));
            var code = reader.ReadToEnd().ReplaceNamespace();
            yield return new Source(resourceName, SourceText.From(code, Encoding.UTF8));
        }
    }
}