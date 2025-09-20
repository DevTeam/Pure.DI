// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
namespace Build.Core.Targets;

using System.Xml.Linq;
using Doc;
using Pure.DI.Benchmarks.Benchmarks;

class ReadmeTarget(
    Commands commands,
    Env env,
    Versions versions,
    Settings settings,
    RootCommand rootCommand,
    ReadmeTools readmeTools,
    Markdown markdown,
    XDocumentTools xDocumentTools,
    FilterTools filterTools,
    [Tag(typeof(CreateExamplesTarget))] ITarget<IReadOnlyCollection<ExampleGroup>> createExamplesTarget,
    [Tag(typeof(BenchmarksTarget))] ITarget<int> benchmarksTarget,
    [Tag(typeof(AIContextTarget))] ITarget<AIContext> aiContextTarget)
    : IInitializable, ITarget<int>
{
    private const string ReadmeDir = "readme";
    private const string CommonReadmeFile = "README.md";
    private const string HeaderTemplateFile = "HeaderTemplate.md";
    private const string ReadmeTemplateFile = "ReadmeTemplate.md";
    private const string FooterTemplateFile = "FooterTemplate.md";
    private const string ContributingTemplateFile = "ContributingTemplate.md";
    private const string ReadmeFile = "README.md";
    private const string ContributingFile = "CONTRIBUTING.md";

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, $"Generate {CommonReadmeFile}", "readme", "r");

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var solutionDirectory = env.GetPath(PathType.SolutionDirectory);
        var logsDirectory = Path.Combine(solutionDirectory, ".logs");

        // Run benchmarks
        await benchmarksTarget.RunAsync(cancellationToken);

        // Delete generated files
        var generatedFiles = Path.Combine(logsDirectory, "Pure.DI", "Pure.DI.SourceGenerator");
        if (Directory.Exists(generatedFiles))
        {
            Directory.Delete(generatedFiles, true);
        }

        var examples = await createExamplesTarget.RunAsync(cancellationToken);

        await using var readmeWriter = File.CreateText(ReadmeFile);

        await AddContentAsync(HeaderTemplateFile, readmeWriter);

        await AddContentAsync(CommonReadmeFile, readmeWriter, "docs");

        await AddContentAsync(ReadmeTemplateFile, readmeWriter);

        await AddAPIAsync(readmeWriter);

        await GenerateExamplesAsync(examples, readmeWriter, logsDirectory);

        await AddContentAsync(FooterTemplateFile, readmeWriter);

        await AddAIContextAsync(readmeWriter, cancellationToken);

        await AddContributingAsync(readmeWriter);

        await AddBenchmarksAsync(logsDirectory, readmeWriter);

        await readmeWriter.FlushAsync(cancellationToken);

        await using var contributingWriter = File.CreateText(ContributingFile);

        await AddContributingAsync(contributingWriter);

        await contributingWriter.FlushAsync(cancellationToken);

        return 0;
    }

    private async Task AddAPIAsync(StreamWriter writer)
    {
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("## API");
        await writer.WriteLineAsync();
        var sourceDirectory = env.GetPath(PathType.SourceDirectory);
        var xmlDocFile = Path.Combine(sourceDirectory, "Pure.DI.Core", "bin", settings.Configuration, "netstandard2.0", "Pure.DI.xml");
        using var xmlDocReader = File.OpenText(xmlDocFile);
        var xmlDoc = await xDocumentTools.LoadAsync(xmlDocReader, LoadOptions.None, CancellationToken.None);
        await markdown.ConvertAsync(xmlDoc, writer, part => filterTools.DocumentPartFilter(AIContextSize.Large, part), CancellationToken.None);
    }

    private async Task AddAIContextAsync(StreamWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("## AI Context");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("Contextual AI needs to understand the situation it’s in. This means knowing details like API, usage scenarios, etc. This helps the AI give more relevant and personalized responses. So Markdown docs below can be useful if you or your team rely on an AI assistant to write code using Pure.DI:");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("| AI Context file | Size | Tokens |");
        await writer.WriteLineAsync("| --------------- | ---- | ------ |");
        var aiContext = await aiContextTarget.RunAsync(cancellationToken);
        foreach (var aiContextFile in aiContext.Files)
        {
            var fileName = Path.GetFileName(aiContextFile.FileName);
            await writer.WriteLineAsync($"| [{fileName}]({fileName}) | {aiContextFile.SizeKB}KB | {aiContextFile.SizeKTokens}K |");
        }
    }

    private async Task AddContributingAsync(StreamWriter writer)
    {
        await AddContentAsync(ContributingTemplateFile, writer, onLine: async line => {
            switch (line.ToLowerInvariant().Trim())
            {
                case "$(commands)":
                    foreach (var command in rootCommand.Subcommands.OrderBy(i => i.Name))
                    {
                        await writer.WriteLineAsync($"| {string.Join(", ", command.Aliases.OrderBy(i => i.Length).ThenBy(i => i))} | {command.Description} |");
                    }

                    return true;

                default:
                    return false;
            }
        });
    }

    private static async Task AddContentAsync(string sourceFile, TextWriter writer, string readmeDir = ReadmeDir, Func<string, Task<bool>>? onLine = null)
    {
        Info($"Adding a content from \"{sourceFile}\"");
        foreach (var line in await File.ReadAllLinesAsync(Path.Combine(readmeDir, sourceFile)))
        {
            if (onLine == null || !await onLine(line))
            {
                await writer.WriteLineAsync(line);
            }
        }
    }

    private async Task GenerateExamplesAsync(IReadOnlyCollection<ExampleGroup> examples, TextWriter writer, string logsDirectory)
    {
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
                    await File.WriteAllTextAsync(readmeFile.Replace("PageTemplate.md", ".md"), content);
                }

                continue;
            }

            File.Delete(readmeFile);
        }

        await writer.WriteLineAsync("## Examples");
        await writer.WriteLineAsync();

        foreach (var (groupName, exampleItems) in examples)
        {
            var groupTitle = new string(readmeTools.FormatTitle(groupName).ToArray());
            Info($"Processing examples group \"{groupTitle}\"");
            await writer.WriteLineAsync($"### {groupTitle}");
            foreach (var example in exampleItems)
            {
                var description = example[CreateExamplesTarget.DescriptionKey];
                var code = example[CreateExamplesTarget.BodyKey];
                var file = CreateExampleFileName(description);
                var exampleFile = $"{file}.md";
                await using var examplesWriter = File.CreateText(Path.Combine(ReadmeDir, exampleFile));
                WriteLine(description, Color.Details);
                await writer.WriteLineAsync($"- [{description}]({ReadmeDir}/{exampleFile})");
                await examplesWriter.WriteLineAsync($"#### {description}");
                await examplesWriter.WriteLineAsync();
                var header = example[CreateExamplesTarget.HeaderKey];
                if (!string.IsNullOrWhiteSpace(header))
                {
                    await examplesWriter.WriteLineAsync(header);
                    await examplesWriter.WriteLineAsync();
                }

                await examplesWriter.WriteLineAsync();
                await examplesWriter.WriteLineAsync("```c#");
                await examplesWriter.WriteLineAsync(code);
                await examplesWriter.WriteLineAsync("```");
                await examplesWriter.WriteLineAsync();
                await examplesWriter.WriteLineAsync("<details>");
                await examplesWriter.WriteLineAsync("<summary>Running this code sample locally</summary>");
                await examplesWriter.WriteLineAsync();
                await examplesWriter.WriteLineAsync($"- Make sure you have the [.NET SDK {settings.BaseDotNetFrameworkVersion}](https://dotnet.microsoft.com/en-us/download/dotnet/{settings.BaseDotNetFrameworkVersion}) or later is installed");
                await examplesWriter.WriteLineAsync("```bash");
                await examplesWriter.WriteLineAsync("dotnet --list-sdk");
                await examplesWriter.WriteLineAsync("```");
                await examplesWriter.WriteLineAsync($"- Create a net{settings.BaseDotNetFrameworkVersion} (or later) console application");
                await examplesWriter.WriteLineAsync("```bash");
                await examplesWriter.WriteLineAsync("dotnet new console -n Sample");
                await examplesWriter.WriteLineAsync("```");
                var references = example[CreateExamplesTarget.ReferencesKey].Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var refs = references.Length > 0 ? "s" : "";
                await examplesWriter.WriteLineAsync($"- Add reference{refs} to NuGet package{refs}");
                await examplesWriter.WriteLineAsync("  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)");
                foreach (var reference in references)
                {
                    await examplesWriter.WriteLineAsync($"  - [{reference}](https://www.nuget.org/packages/{reference})");
                }
                await examplesWriter.WriteLineAsync("```bash");
                await examplesWriter.WriteLineAsync("dotnet add package Pure.DI");
                foreach (var reference in references)
                {
                    await examplesWriter.WriteLineAsync($"dotnet add package {reference}");
                }
                await examplesWriter.WriteLineAsync("```");
                await examplesWriter.WriteLineAsync("- Copy the example code into the _Program.cs_ file");
                await examplesWriter.WriteLineAsync();
                await examplesWriter.WriteLineAsync("You are ready to run the example 🚀");
                await examplesWriter.WriteLineAsync("```bash");
                await examplesWriter.WriteLineAsync("dotnet run");
                await examplesWriter.WriteLineAsync("```");
                await examplesWriter.WriteLineAsync();
                await examplesWriter.WriteLineAsync("</details>");
                await examplesWriter.WriteLineAsync();

                var footer = example[CreateExamplesTarget.FooterKey];
                if (!string.IsNullOrWhiteSpace(footer))
                {
                    await examplesWriter.WriteLineAsync(footer);
                    await examplesWriter.WriteLineAsync();
                }

                var exampleName = Path.GetFileNameWithoutExtension(example[CreateExamplesTarget.SourceKey]);

                await AddExample(logsDirectory, $"Pure.DI.UsageTests.*.{exampleName}.*.g.cs", examplesWriter);
                await examplesWriter.WriteLineAsync();

                await AddClassDiagram(logsDirectory, exampleName, examplesWriter);
                await examplesWriter.WriteLineAsync();

                await examplesWriter.FlushAsync();
            }
        }
    }

    private static async Task AddClassDiagram(string logsDirectory, string exampleName, TextWriter writer)
    {
        var classDiagramFile = Path.Combine(logsDirectory, exampleName + ".Mermaid");
        if (File.Exists(classDiagramFile))
        {
            await writer.WriteLineAsync("Class diagram:");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("```mermaid");
            var classDiagram = await File.ReadAllTextAsync(classDiagramFile);
            await writer.WriteLineAsync(classDiagram);
            await writer.WriteLineAsync("```");
        }
    }

    private static async Task AddExample(string logsDirectory, string exampleSearchPattern, TextWriter writer)
    {
        var salt = $"{DateTime.Now.DayOfYear}d";
        foreach (var generatedCodeFile in Directory.GetFiles(Path.Combine(logsDirectory, "Pure.DI", "Pure.DI.SourceGenerator"), exampleSearchPattern).OrderBy(i => i))
        {
            var ns = string.Join('.', Path.GetFileName(generatedCodeFile).Split('.').AsEnumerable().Reverse().Skip(3).Reverse()) + ".";
            await writer.WriteLineAsync("The following partial class will be generated:");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("```c#");
            var generatedCode = await File.ReadAllTextAsync(generatedCodeFile);
            generatedCode = string.Join(
                Environment.NewLine,
                generatedCode
                    .Split(Environment.NewLine)
                    .SkipWhile(i => i != "{")
                    .Skip(2)
                    .Reverse()
                    .SkipWhile(i => !i.Contains("public override string ToString()"))
                    .Skip(5)
                    .Reverse()
                    .Concat(["}"])
                    .Where(i => {
                        var line = i.TrimStart();
                        return !(
                            line.StartsWith("///")
                            || line.StartsWith("[global::System.Diagnostics.")
                            || line.EndsWith(" // Code coverage")
                            || line.EndsWith(" // Pure method")
                            || line.StartsWith("#region")
                            || line.StartsWith("#endregion"));
                    })
                    .Select(i => i.Length > 1 && char.IsWhiteSpace(i[0]) ? i[1..].TrimEnd() : i.TrimEnd())
                    .Select(i => i
                        .TrimEnd()
                        .Replace("\t", "  ")
                        .Replace(ns, "")
                        .Replace("global::", "")
                        .Replace("System.Threading.Tasks.", "")
                        .Replace("System.Threading.", "")
                        .Replace("System.Runtime.CompilerServices.", "")
                        .Replace("System.Collections.Generic.", "")
                        .Replace("System.", "")
                        .Replace("Pure.DI.", "")
                        .Replace("Benchmarks.Model.", "")
                        .Replace(salt, "")
                        .Replace("(MethodImplOptions)256", "MethodImplOptions.AggressiveInlining")
                        .Replace("(MethodImplOptions)8", "MethodImplOptions.NoInlining")));

            await writer.WriteLineAsync(generatedCode);
            await writer.WriteLineAsync("```");
        }
    }

    private static async Task AddBenchmarksAsync(string logsDirectory, TextWriter readmeWriter)
    {
        var benchmarksReportFiles = Directory.EnumerateFiles(logsDirectory, "*.html").ToArray();
        if (benchmarksReportFiles.Length != 0)
        {
            await readmeWriter.WriteLineAsync();
            await readmeWriter.WriteLineAsync("## Benchmarks");
            await readmeWriter.WriteLineAsync();
            var files = benchmarksReportFiles.OrderBy(i => i).ToArray();
            for (var fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                var benchmarksReportFile = files[fileIndex];
                var reportName = new string(Path.GetFileNameWithoutExtension(benchmarksReportFile).SkipWhile(ch => ch != ' ').Skip(1).ToArray());
                Info($"Processing benchmarks \"{reportName}\"");
                var lines = await File.ReadAllLinesAsync(benchmarksReportFile);
                await readmeWriter.WriteLineAsync("<details>");
                await readmeWriter.WriteLineAsync($"<summary>{reportName}</summary>");
                await readmeWriter.WriteLineAsync();
                var contentLines = lines
                    .SkipWhile(i => !i.Contains("<table>"))
                    .TakeWhile(i => !i.Contains("</body>"))
                    .Where(i => !i.Contains("<td>NA</td>"));

                foreach (var contentLine in contentLines)
                {
                    await readmeWriter.WriteLineAsync(contentLine.Replace('?', ' '));
                }

                await readmeWriter.WriteLineAsync();
                await readmeWriter.WriteLineAsync($"[{reportName} details]({ReadmeDir}/{reportName}Details.md)");
                await readmeWriter.WriteLineAsync();
                await readmeWriter.WriteLineAsync("</details>");
                await readmeWriter.WriteLineAsync();

                if (fileIndex == files.Length - 1)
                {
                    await readmeWriter.WriteLineAsync("<details>");
                    await readmeWriter.WriteLineAsync("<summary>Benchmarks environment</summary>");
                    await readmeWriter.WriteLineAsync();

                    var headerLines = lines
                        .SkipWhile(i => !i.Contains("<pre><code>"))
                        .TakeWhile(i => !i.Contains("<pre><code></code></pre>"));

                    foreach (var headerLine in headerLines)
                    {
                        await readmeWriter.WriteLineAsync(headerLine);
                    }

                    await readmeWriter.WriteLineAsync();
                    await readmeWriter.WriteLineAsync("</details>");
                }
            }
        }

        var benchmarks = new (string name, string description, string? classDiagram)[]
        {
            (nameof(Transient), "Creating an object graph of 22 transient objects.", new Transient().ToString()),
            (nameof(Singleton), "Creating an object graph of 20 transition objects plus 1 singleton with an additional 6 transition objects .", new Singleton().ToString()),
            (nameof(Func), "Creating an object graph of 7 transition objects plus 1 `Func<T>` with additional 1 transition object.", new Func().ToString()),
            (nameof(Array), "Creating an object graph of 27 transient objects, including 4 transient array objects.", new Array().ToString()),
            (nameof(Enum), "Creating an object graph of 12 transient objects, including 1 transient enumerable object.", new Enum().ToString())
        };

        foreach (var (name, description, classDiagram) in benchmarks)
        {
            await using var classDiagramWriter = File.CreateText(Path.Combine(ReadmeDir, $"{name}Details.md"));
            await classDiagramWriter.WriteLineAsync($"## {name} details");
            await classDiagramWriter.WriteLineAsync();
            await classDiagramWriter.WriteLineAsync(description);
            await classDiagramWriter.WriteLineAsync();
            await classDiagramWriter.WriteLineAsync("### Class diagram");
            await classDiagramWriter.WriteLineAsync("```mermaid");
            await classDiagramWriter.WriteLineAsync(classDiagram);
            await classDiagramWriter.WriteLineAsync("```");

            await classDiagramWriter.WriteLineAsync();
            await classDiagramWriter.WriteLineAsync("### Generated code");
            await classDiagramWriter.WriteLineAsync();
            await AddExample(logsDirectory, $"Pure.DI.Benchmarks.Benchmarks.{name}.g.cs", classDiagramWriter);
        }
    }

    private static string CreateExampleFileName(string text) =>
        text.Replace(" ", "-")
            .Replace("_", string.Empty)
            .Replace("'", string.Empty)
            .Replace("/", string.Empty)
            .Replace("`", string.Empty)
            .Replace("\\", string.Empty)
            .ToLowerInvariant();
}