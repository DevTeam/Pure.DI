// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
namespace Build.Core.Targets;

using System.Xml.Linq;
using Doc;
using Enum=Enum;

class AIContextTarget(
    Commands commands,
    Env env,
    Versions versions,
    Settings settings,
    Markdown markdown,
    XDocumentTools xDocumentTools,
    FilterTools filterTools,
    [Tag(typeof(CreateExamplesTarget))] ITarget<IReadOnlyCollection<ExampleGroup>> createExamplesTarget)
    : IInitializable, ITarget<AIContext>
{
    private const long AITokenSizeBytes = 4;
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

    private async Task<AIContextFile> CreateAiContextFile(
        string fileName,
        IReadOnlyCollection<ExampleGroup> examples,
        AIContextSize size,
        CancellationToken cancellationToken)
    {
        await using var writer = File.CreateText(fileName);

        {
            await writer.WriteLineAsync("This Markdown-formatted document contains information about working with Pure.DI");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("# Usage scenarios.");
            foreach (var (groupName, exampleItems) in examples)
            {
                foreach (var example in exampleItems)
                {
                    var priority = int.Parse(example[CreateExamplesTarget.PriorityKey]);
                    if (!filterTools.AddExample(size, priority, groupName))
                    {
                        continue;
                    }

                    var description = example[CreateExamplesTarget.DescriptionKey];
                    var code = example[CreateExamplesTarget.BodyKey];
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync($"## {description}");
                    await writer.WriteLineAsync();
                    var header = example[CreateExamplesTarget.HeaderKey];
                    if (!string.IsNullOrWhiteSpace(header))
                    {
                        await writer.WriteLineAsync(header);
                        await writer.WriteLineAsync();
                    }

                    await writer.WriteLineAsync("```c#");
                    await writer.WriteLineAsync(code);
                    await writer.WriteLineAsync("```");
                    await writer.WriteLineAsync();
                    var references = example[CreateExamplesTarget.ReferencesKey].Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var refs = references.Length > 0 ? "s" : "";
                    await writer.WriteLineAsync($"To run the above code, the following NuGet package{refs} must be added:");
                    await writer.WriteLineAsync(" - [Pure.DI](https://www.nuget.org/packages/Pure.DI)");
                    foreach (var reference in references)
                    {
                        await writer.WriteLineAsync($" - [{reference}](https://www.nuget.org/packages/{reference})");
                    }

                    await writer.WriteLineAsync();

                    var footer = example[CreateExamplesTarget.FooterKey];
                    if (!string.IsNullOrWhiteSpace(footer))
                    {
                        await writer.WriteLineAsync(footer);
                    }
                }
            }

            if (size >= AIContextSize.Large)
            {
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("# Examples of using Pure.DI for different types of .NET projects.");
                await writer.WriteLineAsync();
                var generatorPackageVersion = versions.GetNext(new NuGetRestoreSettings("Pure.DI"), Settings.VersionRange, 0).ToString();
                var msPackageVersion = versions.GetNext(new NuGetRestoreSettings("Pure.DI.MS"), Settings.VersionRange, 0).ToString();
                foreach (var readmeFile in Directory.EnumerateFiles(Path.Combine(ReadmeDir), "*.md"))
                {
                    if (readmeFile.EndsWith("Template.md", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (readmeFile.EndsWith("PageTemplate.md"))
                        {
                            var content = await File.ReadAllTextAsync(readmeFile, cancellationToken);
                            content = content
                                .Replace("$(version)", generatorPackageVersion)
                                .Replace("$(ms.version)", msPackageVersion)
                                .Replace("$(targetFrameworkVersion)", $"net{settings.BaseDotNetFrameworkVersion}");
                            await writer.WriteLineAsync(content);
                        }
                    }
                }
            }

            if (size >= AIContextSize.Large)
            {
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("# Pure.DI API");
                await writer.WriteLineAsync();
                var sourceDirectory = env.GetPath(PathType.SourceDirectory);
                var xmlDocFile = Path.Combine(sourceDirectory, "Pure.DI.Core", "bin", settings.Configuration, "netstandard2.0", "Pure.DI.xml");
                using var xmlDocReader = File.OpenText(xmlDocFile);
                var xmlDoc = await xDocumentTools.LoadAsync(xmlDocReader, LoadOptions.None, CancellationToken.None);
                await markdown.ConvertAsync(xmlDoc, writer, part => filterTools.DocumentPartFilter(size, part), CancellationToken.None);
            }

            await writer.FlushAsync(cancellationToken);
        }

        var sizeBytes = new FileInfo(fileName).Length;
        var sizeKTokens = sizeBytes / AITokenSizeBytes / 1000;
        if (sizeKTokens >= (long)size)
        {
            Error($"{size} {Path.GetFileName(fileName)} has {sizeKTokens}K/{(long)size}K tokens.");
        }
        else
        {
            Summary($"{size} {Path.GetFileName(fileName)} has {sizeKTokens}K/{(long)size}K tokens.");
        }

        return new AIContextFile(fileName, size, sizeBytes / 1024L, sizeKTokens);
    }
}