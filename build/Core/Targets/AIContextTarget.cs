// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

using System.Xml.Linq;
using Doc;
using Enum=Enum;

class AiContextTarget(
    Commands commands,
    Env env,
    Versions versions,
    Settings settings,
    Markdown markdown,
    XDocumentTools xDocumentTools,
    ReadmeTools readmeTools,
    [Tag(typeof(CreateExamplesTarget))] ITarget<IReadOnlyCollection<ExampleGroup>> createExamplesTarget)
    : IInitializable, ITarget<AIContext>
{
    private const string ReadmeDir = "readme";
    private const string AiContextReadmeFileTemplate = "AI_CONTEXT_{0}.md";

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Generate AI context", "ai");

    public async Task<AIContext> RunAsync(CancellationToken cancellationToken)
    {
        var examples = await createExamplesTarget.RunAsync(cancellationToken);
        var aiContextFileTasks = Enum.GetValues<AIContextSize>()
            .Select(size => CreateAiContextFile(
                string.Format(AiContextReadmeFileTemplate, size.ToString().ToUpperInvariant()),
                examples,
                size,
                cancellationToken));

        var files = await Task.WhenAll(aiContextFileTasks);
        return new AIContext(files);
    }

    private async Task<AIContextFile> CreateAiContextFile(string fileName, IReadOnlyCollection<ExampleGroup> examples, AIContextSize size, CancellationToken cancellationToken)
    {
        await using var writer = File.CreateText(fileName);
        {
            await writer.WriteLineAsync("# Pure.DI source code generator usage scenarios.");
            foreach (var (groupName, exampleItems) in examples)
            {
                await writer.WriteLineAsync();
                var groupTitle = new string(readmeTools.FormatTitle(groupName).ToArray());
                await writer.WriteLineAsync($"## {groupTitle} scenarios");
                foreach (var vars in exampleItems)
                {
                    var description = vars[CreateExamplesTarget.DescriptionKey];
                    var code = vars[CreateExamplesTarget.BodyKey];
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync($"### {description}");
                    await writer.WriteLineAsync();
                    var header = vars[CreateExamplesTarget.HeaderKey];
                    if (!string.IsNullOrWhiteSpace(header))
                    {
                        await writer.WriteLineAsync(header);
                        await writer.WriteLineAsync();
                    }

                    await writer.WriteLineAsync("```c#");
                    await writer.WriteLineAsync(code);
                    await writer.WriteLineAsync("```");
                    await writer.WriteLineAsync();
                    var references = vars[CreateExamplesTarget.ReferencesKey].Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var refs = references.Length > 0 ? "s" : "";
                    await writer.WriteLineAsync($"To run the above code, the following NuGet package{refs} must be added:");
                    await writer.WriteLineAsync(" - [Pure.DI](https://www.nuget.org/packages/Pure.DI)");
                    foreach (var reference in references)
                    {
                        await writer.WriteLineAsync($"  - [{reference}](https://www.nuget.org/packages/{reference})");
                    }

                    await writer.WriteLineAsync();

                    var footer = vars[CreateExamplesTarget.FooterKey];
                    if (!string.IsNullOrWhiteSpace(footer))
                    {
                        await writer.WriteLineAsync(footer);
                    }
                }
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync("# Examples of using Pure.DI source code generator in different types of .NET applications.");
            await writer.WriteLineAsync();
            var generatorPackageVersion = versions.GetNext(new NuGetRestoreSettings("Pure.DI"), Settings.VersionRange, 0).ToString();
            var msPackageVersion = versions.GetNext(new NuGetRestoreSettings("Pure.DI.MS"), Settings.VersionRange, 0).ToString();
            foreach (var readmeFile in Directory.EnumerateFiles(Path.Combine(ReadmeDir), "*.md"))
            {
                if (readmeFile.EndsWith("Template.md", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (readmeFile.EndsWith("PageTemplate.md"))
                    {
                        var content = await File.ReadAllTextAsync(readmeFile);
                        content = content
                            .Replace("$(version)", generatorPackageVersion)
                            .Replace("$(ms.version)", msPackageVersion)
                            .Replace("$(targetFrameworkVersion)", $"net{settings.BaseDotNetFrameworkVersion}");
                        await writer.WriteLineAsync(content);
                    }
                }
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync("# Pure.DI API");
            await writer.WriteLineAsync();
            var sourceDirectory = env.GetPath(PathType.SourceDirectory);
            var xmlDocFile = Path.Combine(sourceDirectory, "Pure.DI.Core", "bin", settings.Configuration, "netstandard2.0", "Pure.DI.xml");
            using var xmlDocReader = File.OpenText(xmlDocFile);
            var xmlDoc = await xDocumentTools.LoadAsync(xmlDocReader, LoadOptions.None, CancellationToken.None);
            await markdown.ConvertAsync(
                xmlDoc,
                writer,
                i => i.NamespaceName == "Pure.DI" && i.TypeName != "Generator",
                CancellationToken.None);

            await writer.FlushAsync(cancellationToken);
        }

        var sizeBytes = new FileInfo(fileName).Length;
        return new AIContextFile(fileName, size, sizeBytes);
    }
}