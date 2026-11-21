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
    RazorTemplateEngine templateEngine,
    [Tag(typeof(CreateExamplesTarget))] ITarget<IReadOnlyCollection<ExampleGroup>> createExamplesTarget)
    : IInitializable, ITarget<int>
{
    private const string ReadmeDir = "readme";
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
        this, "Generate README.md", "readme", "r");

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var solutionDirectory = env.GetPath(PathType.SolutionDirectory);
        var logsDirectory = Path.Combine(solutionDirectory, ".logs");
        var readmeDirectory = Path.Combine(solutionDirectory, ReadmeDir);
        var generatedCodeDirectory = Path.Combine(logsDirectory, "Pure.DI", "Pure.DI.SourceGenerator");
        if (Directory.Exists(generatedCodeDirectory))
        {
            Directory.Delete(generatedCodeDirectory, true);
        }

        var examples = await createExamplesTarget.RunAsync(cancellationToken);
        var filesByDescription = examples
            .SelectMany(group => group.Examples)
            .Select(example => example[CreateExamplesTarget.DescriptionKey])
            .Distinct()
            .ToDictionary(description => description, description => $"{CreateExampleFileName(description)}.md");

        var groups = new List<ReadmeGroup>();
        var pages = new Dictionary<string, ExamplePage>(StringComparer.OrdinalIgnoreCase);
        foreach (var (groupName, items) in examples)
        {
            var title = new string(readmeTools.FormatTitle(groupName).ToArray());
            var links = new List<ReadmeLink>();
            foreach (var example in items)
            {
                var description = example[CreateExamplesTarget.DescriptionKey];
                var file = filesByDescription[description];
                var link = new ReadmeLink(description, file);
                links.Add(link);
                pages[file] = await CreateExamplePageAsync(example, link, filesByDescription, logsDirectory, generatedCodeDirectory, cancellationToken);
            }

            groups.Add(new ReadmeGroup(title, GroupDescriptions.GetValueOrDefault(groupName), links));
        }

        var commandsModel = rootCommand.Subcommands
            .OrderBy(command => command.Name)
            .Select(command => new ReadmeCommand(
                string.Join(", ", command.Aliases.OrderBy(alias => alias.Length).ThenBy(alias => alias)),
                command.Description))
            .ToArray();
        var contributing = await templateEngine.RenderAsync("/Templates/Contributing.cshtml", commandsModel, cancellationToken);
        var readme = new ReadmeModel(
            await ReadMarkdownAsync(Path.Combine(readmeDirectory, "HeaderTemplate.md"), cancellationToken),
            await ReadMarkdownAsync(Path.Combine(solutionDirectory, "docs", "README.md"), cancellationToken),
            await ReadMarkdownAsync(Path.Combine(readmeDirectory, "ReadmeTemplate.md"), cancellationToken),
            groups,
            await ReadMarkdownAsync(Path.Combine(readmeDirectory, "FooterTemplate.md"), cancellationToken),
            contributing);

        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(solutionDirectory, "README.md")] =
                await templateEngine.RenderAsync("/Templates/Readme.cshtml", readme, cancellationToken),
            [Path.Combine(solutionDirectory, "CONTRIBUTING.md")] = contributing
        };
        foreach (var (file, page) in pages)
        {
            output[Path.Combine(readmeDirectory, file)] =
                await templateEngine.RenderAsync("/Templates/Example.cshtml", page, cancellationToken);
        }

        const string pageTemplateSuffix = "PageTemplate.md";
        var packageVersion = settings.CurrentVersion.ToString();
        foreach (var path in Directory.EnumerateFiles(readmeDirectory, $"*{pageTemplateSuffix}"))
        {
            var file = Path.GetFileName(path)[..^pageTemplateSuffix.Length] + ".md";
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            output[Path.Combine(readmeDirectory, file)] = content
                .Replace("$(version)", packageVersion)
                .Replace("$(ms.version)", packageVersion)
                .Replace("$(targetFrameworkVersion)", $"net{settings.BaseDotNetFrameworkVersion}");
        }

        foreach (var (path, content) in output)
        {
            await File.WriteAllTextAsync(path, content, cancellationToken);
            Info($"Generated: {path}");
        }

        foreach (var path in Directory.EnumerateFiles(readmeDirectory, "*.md"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!path.EndsWith("Template.md", StringComparison.OrdinalIgnoreCase) && !output.ContainsKey(path))
            {
                File.Delete(path);
            }
        }

        return 0;
    }

    private static async Task<string> ReadMarkdownAsync(string path, CancellationToken cancellationToken) =>
        string.Concat((await File.ReadAllLinesAsync(path, cancellationToken)).Select(line => line + Environment.NewLine));

    private async Task<ExamplePage> CreateExamplePageAsync(
        Example example,
        ReadmeLink link,
        IReadOnlyDictionary<string, string> filesByDescription,
        string logsDirectory,
        string generatedCodeDirectory,
        CancellationToken cancellationToken)
    {
        var exampleName = Path.GetFileNameWithoutExtension(example[CreateExamplesTarget.SourceKey]);
        var frameworkVersion = example[CreateExamplesTarget.DotNetFrameworkVersionKey];
        var relatedExamples = new List<ReadmeLink>();
        foreach (var title in example[CreateExamplesTarget.SeeAlsoKey]
                     .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (filesByDescription.TryGetValue(title, out var file))
            {
                relatedExamples.Add(new ReadmeLink(title, file));
            }
            else
            {
                Warning($"The example \"{link.Title}\" has an unknown \"see also\" reference \"{title}\"");
            }
        }

        return new ExamplePage(
            link.Title,
            example[CreateExamplesTarget.HeaderKey],
            example[CreateExamplesTarget.BodyKey],
            example[CreateExamplesTarget.FooterKey],
            string.IsNullOrWhiteSpace(frameworkVersion) ? settings.BaseDotNetFrameworkVersion : frameworkVersion,
            example[CreateExamplesTarget.ReferencesKey].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            await ReadGeneratedCodeAsync(generatedCodeDirectory, $"Pure.DI.UsageTests.*.{exampleName}.*.g.cs", cancellationToken),
            await ReadClassDiagramAsync(logsDirectory, exampleName, cancellationToken),
            relatedExamples);
    }

    private static async Task<string?> ReadClassDiagramAsync(string logsDirectory, string exampleName, CancellationToken cancellationToken)
    {
        var classDiagramFile = Path.Combine(logsDirectory, exampleName + ".Mermaid");
        if (!File.Exists(classDiagramFile))
        {
            return null;
        }

        var classDiagram = await File.ReadAllTextAsync(classDiagramFile, cancellationToken);
        var trimmed = classDiagram.TrimStart();
        if (trimmed.Length == 0 || (!trimmed.StartsWith("---") && !trimmed.StartsWith("classDiagram")))
        {
            return null;
        }

        return classDiagram.Replace(Salt, "");
    }

    private static async Task<IReadOnlyList<string>> ReadGeneratedCodeAsync(
        string generatedCodeDirectory,
        string exampleSearchPattern,
        CancellationToken cancellationToken)
    {
        var code = new List<string>();
        foreach (var generatedCodeFile in Directory.GetFiles(generatedCodeDirectory, exampleSearchPattern).OrderBy(path => path))
        {
            var ns = string.Join('.', Path.GetFileName(generatedCodeFile).Split('.').AsEnumerable().Reverse().Skip(3).Reverse()) + ".";
            var generatedCode = await File.ReadAllTextAsync(generatedCodeFile, cancellationToken);
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

            code.Add(generatedCode);
        }

        return code;
    }

    // Characters, that are not allowed in a file name on any supported OS.
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    private static string CreateExampleFileName(string text) =>
        new string(text
            .Replace(" ", "-")
            .Replace("_", string.Empty)
            .Replace("'", string.Empty)
            .Replace("/", string.Empty)
            .Replace("`", string.Empty)
            .Replace("\\", string.Empty)
            .Where(i => !InvalidFileNameChars.Contains(i))
            .ToArray())
            .ToLowerInvariant();
}
