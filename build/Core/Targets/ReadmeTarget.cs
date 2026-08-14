// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeRedundantParentheses
namespace Build.Core.Targets;

class ReadmeTarget(
    Commands commands,
    Env env,
    Settings settings,
    RootCommand rootCommand,
    ReadmeTools readmeTools,
    [Tag(typeof(CreateExamplesTarget))] ITarget<IReadOnlyCollection<ExampleGroup>> createExamplesTarget,
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
    private static readonly string Salt = $"{DateTime.Now.DayOfYear}d";
    private static readonly Dictionary<string, string> GroupDescriptions = new(StringComparer.Ordinal)
    {
        ["QuickStart"] = "A short path through the smallest examples that show how a composition is declared, generated, and used.",
        ["Basics"] = "Core Pure.DI concepts: bindings, roots, arguments, members, and everyday object graph construction.",
        ["Lifetimes"] = "Lifetime choices and disposal patterns for controlling how long generated instances are reused.",
        ["BaseClassLibrary"] = "Built-in support for common .NET types such as arrays, delegates, tasks, spans, service providers, and collections.",
        ["HighPerformance"] = "Scenarios focused on reducing allocations, keeping hot paths explicit, avoiding shared override state, and using generated code for stack-only or deferred values.",
        ["Generics"] = "Generic bindings, roots, type arguments, constraints, and advanced generic graph shapes.",
        ["Attributes"] = "Attribute-based setup options for declaring bindings, tags, metadata, and injection sites near the application code.",
        ["Interception"] = "Decorator and interception examples for wrapping services without moving cross-cutting behavior into consumers.",
        ["Hints"] = "Code generation hints that tune diagnostics, generated APIs, thread-safety, names, and fallback behavior.",
        ["Interfaces"] = "Generated interface scenarios for exposing a stable composition API while keeping implementation details generated.",
        ["Advanced"] = "Less common but practical composition techniques for overrides, builders, dependent compositions, setup context, and tracking.",
        ["UseCases"] = "End-to-end integrations and application-style examples that show Pure.DI in realistic project contexts.",
        ["Unity"] = "Unity-specific composition patterns for scenes, prefabs, and editor-friendly dependency injection."
    };

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, $"Generate {CommonReadmeFile}", "readme", "r");

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var solutionDirectory = env.GetPath(PathType.SolutionDirectory);
        var logsDirectory = Path.Combine(solutionDirectory, ".logs");

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

        await GenerateExamplesAsync(examples, readmeWriter, logsDirectory);

        await AddContentAsync(FooterTemplateFile, readmeWriter);

        await AddAIContextAsync(readmeWriter, cancellationToken);

        await AddContributingAsync(readmeWriter);

        await readmeWriter.FlushAsync(cancellationToken);

        await using var contributingWriter = File.CreateText(ContributingFile);

        await AddContributingAsync(contributingWriter);

        await contributingWriter.FlushAsync(cancellationToken);

        return 0;
    }

    private async Task AddAIContextAsync(StreamWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("## AI Context");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("AI needs to understand the situation it’s in (context). This means knowing details like API, usage scenarios, etc. This helps the AI give more relevant and personalized responses. So Markdown docs below can be useful if you or your team rely on an AI assistant to write code using Pure.DI:");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("| AI context file | Size | Tokens |");
        await writer.WriteLineAsync("| --------------- | ---- | ------ |");
        var aiContext = await aiContextTarget.RunAsync(cancellationToken);
        foreach (var aiContextFile in aiContext.Files)
        {
            var fileName = Path.GetFileName(aiContextFile.FileName);
            await writer.WriteLineAsync($"| [{fileName}]({fileName}) | {aiContextFile.SizeKB}KB | {aiContextFile.SizeKTokens}K |");
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("For different IDEs, you can use the _AGENTS.md_ file as is by simply copying it to the root directory. For use with _JetBrains Rider_ and _Junie_, please refer to [these instructions](https://www.jetbrains.com/help/junie/customize-guidelines.html). For example, you can copy any _AGENTS.md_ file into your project (using _Pure.DI_) as _.junie/guidelines.md._");
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
        var packageVersion = settings.CurrentVersion.ToString();
        foreach (var readmeFile in Directory.EnumerateFiles(Path.Combine(ReadmeDir), "*.md"))
        {
            if (readmeFile.EndsWith("Template.md", StringComparison.InvariantCultureIgnoreCase))
            {
                if (readmeFile.EndsWith("PageTemplate.md"))
                {
                    var content = await File.ReadAllTextAsync(readmeFile);
                    content = content
                        .Replace("$(version)", packageVersion)
                        .Replace("$(ms.version)", packageVersion)
                        .Replace("$(targetFrameworkVersion)", $"net{settings.BaseDotNetFrameworkVersion}");
                    await File.WriteAllTextAsync(readmeFile.Replace("PageTemplate.md", ".md"), content);
                }

                continue;
            }

            File.Delete(readmeFile);
        }

        await writer.WriteLineAsync("## Examples");
        await writer.WriteLineAsync();

        var exampleFilesByDescription = examples
            .SelectMany(group => group.Examples)
            .Select(example => example[CreateExamplesTarget.DescriptionKey])
            .Distinct()
            .ToDictionary(description => description, description => $"{CreateExampleFileName(description)}.md");

        foreach (var (groupName, exampleItems) in examples)
        {
            var groupTitle = new string(readmeTools.FormatTitle(groupName).ToArray());
            Info($"Processing examples group \"{groupTitle}\"");
            await writer.WriteLineAsync($"### {groupTitle}");
            if (GroupDescriptions.TryGetValue(groupName, out var groupDescription))
            {
                await writer.WriteLineAsync();
                await writer.WriteLineAsync(groupDescription);
                await writer.WriteLineAsync();
            }

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
                var dotNetFrameworkVersion = example[CreateExamplesTarget.DotNetFrameworkVersionKey];
                if (string.IsNullOrWhiteSpace(dotNetFrameworkVersion))
                {
                    dotNetFrameworkVersion = settings.BaseDotNetFrameworkVersion;
                }

                await examplesWriter.WriteLineAsync($"- Make sure you have the [.NET SDK {dotNetFrameworkVersion}](https://dotnet.microsoft.com/en-us/download/dotnet/{dotNetFrameworkVersion}) or later installed");
                await examplesWriter.WriteLineAsync("```bash");
                await examplesWriter.WriteLineAsync("dotnet --list-sdk");
                await examplesWriter.WriteLineAsync("```");
                await examplesWriter.WriteLineAsync($"- Create a net{dotNetFrameworkVersion} (or later) console application");
                await examplesWriter.WriteLineAsync("```bash");
                await examplesWriter.WriteLineAsync("dotnet new console -n Sample");
                await examplesWriter.WriteLineAsync("```");
                var references = example[CreateExamplesTarget.ReferencesKey].Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var refs = references.Length > 0 ? "s" : "";
                var art = references.Length > 0 ? "" : "a ";
                await examplesWriter.WriteLineAsync($"- Add {art}reference{refs} to the NuGet package{refs}");
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

                await AddSeeAlso(example, exampleFilesByDescription, examplesWriter);

                await examplesWriter.FlushAsync();
            }
        }
    }

    private static async Task AddSeeAlso(Example example, Dictionary<string, string> exampleFilesByDescription, TextWriter writer)
    {
        var references = example[CreateExamplesTarget.SeeAlsoKey]
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (references.Length == 0)
        {
            return;
        }

        await writer.WriteLineAsync("See also:");
        await writer.WriteLineAsync();
        foreach (var reference in references)
        {
            if (exampleFilesByDescription.TryGetValue(reference, out var referenceFile))
            {
                await writer.WriteLineAsync($"- [{reference}]({referenceFile})");
            }
            else
            {
                Warning($"The example \"{example[CreateExamplesTarget.DescriptionKey]}\" has an unknown \"see also\" reference \"{reference}\"");
            }
        }

        await writer.WriteLineAsync();
    }

    private static async Task AddClassDiagram(string logsDirectory, string exampleName, TextWriter writer)
    {
        var classDiagramFile = Path.Combine(logsDirectory, exampleName + ".Mermaid");
        if (!File.Exists(classDiagramFile))
        {
            return;
        }

        var classDiagram = await File.ReadAllTextAsync(classDiagramFile);
        var trimmed = classDiagram.TrimStart();
        if (trimmed.Length == 0 || (!trimmed.StartsWith("---") && !trimmed.StartsWith("classDiagram")))
        {
            return;
        }

        classDiagram = classDiagram.Replace(Salt, "");

        await writer.WriteLineAsync("Class diagram:");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("```mermaid");
        await writer.WriteLineAsync(classDiagram);
        await writer.WriteLineAsync("```");
    }

    private static async Task AddExample(string logsDirectory, string exampleSearchPattern, TextWriter writer)
    {
        foreach (var generatedCodeFile in Directory.GetFiles(Path.Combine(logsDirectory, "Pure.DI", "Pure.DI.SourceGenerator"), exampleSearchPattern).OrderBy(i => i))
        {
            var ns = string.Join('.', Path.GetFileName(generatedCodeFile).Split('.').AsEnumerable().Reverse().Skip(3).Reverse()) + ".";
            await writer.WriteLineAsync("<details>");
            await writer.WriteLineAsync("<summary>The following partial class will be generated</summary>");
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
                            || line.StartsWith("[global::System.CodeDom.Compiler.")
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
                        .Replace(Salt, "")
                        .Replace("(MethodImplOptions)256", "MethodImplOptions.AggressiveInlining")
                        .Replace("(MethodImplOptions)8", "MethodImplOptions.NoInlining")
                        .Replace("[NonSerializedAttribute] ", "")));

            await writer.WriteLineAsync(generatedCode);
            await writer.WriteLineAsync("```");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("</details>");
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
