namespace Pure.DI.Core;

using System.IO;
using System.Text.RegularExpressions;

internal class SourceSet
{
    private static readonly List<Source> Features;
    private static readonly List<Source> Api;

    public readonly IReadOnlyList<Source> ApiSources;
    public readonly IEnumerable<SyntaxTree> FeaturesTrees;
    public readonly IEnumerable<SyntaxTree> ApiTrees;

    static SourceSet()
    {
        Regex featuresRegex = new(@"Pure.DI.Features.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        Regex componentsRegex = new(@"Pure.DI.Components.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        Features = GetResources(featuresRegex, SourceType.Feature).ToList();
        Api = GetResources(componentsRegex, SourceType.Api).ToList();
    }

    public SourceSet(CSharpParseOptions parseOptions)
    {
        ApiSources = Api;
        FeaturesTrees = Features.Select(source => CSharpSyntaxTree.ParseText(source.Code, parseOptions));
        ApiTrees = Api.Select(source => CSharpSyntaxTree.ParseText(source.Code, parseOptions));
    }

    private static IEnumerable<Source> GetResources(Regex filter, SourceType sourceType)
    {
        var assembly = typeof(SourceSet).Assembly;
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!filter.IsMatch(resourceName))
            {
                continue;
            }

            using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Cannot read {resourceName}."));
            var code = reader.ReadToEnd();
            yield return new Source(sourceType, resourceName, SourceText.From(code, Encoding.UTF8));
        }
    }
}