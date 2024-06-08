// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ClassNeverInstantiated.Global

namespace Build;

using Pure.DI.Benchmarks.Benchmarks;

internal class ReadmeTarget(
    Commands commands,
    Env env,
    Versions versions,
    [Tag(typeof(BenchmarksTarget))] ITarget<int> benchmarksTarget)
    : IInitializable, ITarget<int>
{
    private const string ReadmeDir = "readme";
    private const string ReadmeHeaderFile = "README.md";
    private const string ReadmeTemplateFile = "ReadmeTemplate.md";
    private const string FooterTemplateFile = "FooterTemplate.md";
    private const string ReadmeFile = "README.md";
    private const string VisibleKey = "v";
    private const string TitleKey = "t";
    private const string PriorityKey = "p";
    private const string DescriptionKey = "d";
    private const string HeaderKey = "h";
    private const string FooterKey = "f";
    private const string SourceKey = "s";
    private const string BodyKey = "b";

    private static readonly string[] Groups =
    [
        "Basics",
        "Lifetimes",
        "BaseClassLibrary",
        "Generics",
        "Attributes",
        "Interception",
        "Hints",
        "Advanced"
    ];

    private static readonly char[] Separator = ['='];

    public Task InitializeAsync() => commands.Register(
        this,
        $"Generates {ReadmeHeaderFile}",
        "readme",
        "r");

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

        var usageTestsProjects = Path.Combine(solutionDirectory, "tests", "Pure.DI.UsageTests", "Pure.DI.UsageTests.csproj");
        var projects = new[]
        {
            usageTestsProjects,
            Path.Combine(solutionDirectory, "benchmarks", "Pure.DI.Benchmarks", "Pure.DI.Benchmarks.csproj")
        };

        foreach (var project in projects)
        {
            new MSBuild()
                .WithProject(project)
                .WithTarget("clean;rebuild")
                .Build()
                .Succeed();
        }

        new DotNetTest(usageTestsProjects).Run();

        await using var readmeWriter = File.CreateText(ReadmeFile);

        await AddContent(ReadmeHeaderFile, readmeWriter, "docs");

        await AddContent(ReadmeTemplateFile, readmeWriter);

        await readmeWriter.WriteLineAsync("");

        var examples = await CreateExamples(cancellationToken);
        await GenerateExamples(examples, readmeWriter, logsDirectory);

        await AddContent(FooterTemplateFile, readmeWriter);

        await readmeWriter.WriteLineAsync("");

        await AddBenchmarks(logsDirectory, readmeWriter);

        await readmeWriter.FlushAsync(cancellationToken);
        return 0;
    }

    private static async Task AddContent(string sourceFile, TextWriter readmeWriter, string readmeDir = ReadmeDir)
    {
        Info($"Adding a content from \"{sourceFile}\"");
        foreach (var line in await File.ReadAllLinesAsync(Path.Combine(readmeDir, sourceFile)))
        {
            await readmeWriter.WriteLineAsync(line);
        }
    }

    private async Task<IEnumerable<(string GroupName, Dictionary<string, string>[] SampleItems)>> CreateExamples(CancellationToken cancellationToken)
    {
        var items = new List<Dictionary<string, string>>();
        var testsDir = Path.Combine(env.GetPath(PathType.SolutionDirectory), "tests", "Pure.DI.UsageTests");
        var files = Directory.EnumerateFiles(testsDir, "*.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(Environment.CurrentDirectory, file);
            Part? part = default;
            var vars = new Dictionary<string, string>
            {
                [VisibleKey] = "False",
                [TitleKey] = Path.GetDirectoryName(Path.GetRelativePath(testsDir, file)) ?? "",
                [PriorityKey] = string.Empty,
                [DescriptionKey] = string.Empty,
                [HeaderKey] = string.Empty,
                [FooterKey] = string.Empty,
                [SourceKey] = relativePath
            };
            items.Add(vars);
            var body = new List<string>();
            var localBody = new List<string>();
            var offset = int.MaxValue;
            foreach (var line in await File.ReadAllLinesAsync(file, cancellationToken))
            {
                var str = line.Trim().Replace(" ", "");
                if (str.StartsWith("/*"))
                {
                    part = Part.Comment;
                    continue;
                }

                if (str.StartsWith("*/"))
                {
                    part = default;
                    continue;
                }

                if (str.StartsWith("//{"))
                {
                    part = Part.Body;
                    continue;
                }

                if (str.StartsWith("//}"))
                {
                    if (body.Count != 0)
                    {
                        body.Add("");
                    }

                    body.AddRange(localBody.Select(i => i.Length > offset ? i[offset..].TrimEnd() : i));
                    offset = int.MaxValue;
                    localBody.Clear();
                    part = default;
                    continue;
                }

                if (part == Part.Comment && str.StartsWith('$'))
                {
                    var parts = line[1..].Split(Separator, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        var key = parts[0].Trim();
                        var curVar = vars[key];
                        if (curVar != string.Empty)
                        {
                            curVar += "\n";
                        }

                        if (parts.Length == 2)
                        {
                            vars[key] = curVar + parts[1].TrimEnd();
                        }
                        else
                        {
                            vars[key] = curVar;
                        }
                    }

                    continue;
                }

                if (part == Part.Body)
                {
                    var trimmedLine = line.TrimStart();
                    if (!string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        var curOffset = line.Length - trimmedLine.Length;
                        if (curOffset < offset)
                        {
                            offset = curOffset;
                        }
                    }

                    localBody.Add(line);
                }
            }

            if (body.Count != 0)
            {
                vars[BodyKey] = string.Join(Environment.NewLine, body);
            }
        }

        var groups = Groups
            .Select((name, index) => (name, index))
            .ToDictionary(i => i.name, i => i.index);

        var examples = items
            .Where(i => i.ContainsKey(BodyKey) && i[VisibleKey] != "False")
            .GroupBy(i => i[TitleKey])
            .OrderBy(i => groups.TryGetValue(i.Key, out var index) ? index : int.MaxValue)
            .Select(i => (GroupName: i.Key, SampleItems: i.OrderBy(j => int.Parse(j[PriorityKey])).ThenBy(j => j[DescriptionKey]).ToArray()));

        return examples;
    }

    private async Task GenerateExamples(IEnumerable<(string GroupName, Dictionary<string, string>[] SampleItems)> examples, TextWriter readmeWriter, string logsDirectory)
    {
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
                        .Replace("$(targetFrameworkVersion)", "net8.0");
                    await File.WriteAllTextAsync(readmeFile.Replace("PageTemplate.md", ".md"), content);
                }

                continue;
            }

            File.Delete(readmeFile);
        }

        await readmeWriter.WriteLineAsync("## Examples");
        await readmeWriter.WriteLineAsync("");
        foreach (var (groupName, exampleItems) in examples)
        {
            var groupTitle = new string(FormatTitle(groupName).ToArray());
            Info($"Processing examples group \"{groupTitle}\"");
            await readmeWriter.WriteLineAsync($"### {groupTitle}");
            foreach (var vars in exampleItems)
            {
                var description = vars[DescriptionKey];
                var exampleFile = $"{CreateExampleFileName(description)}.md";
                await using var examplesWriter = File.CreateText(Path.Combine(ReadmeDir, exampleFile));
                WriteLine($"  · \"{description}\"", Color.Details);
                await readmeWriter.WriteLineAsync($"- [{description}]({ReadmeDir}/{exampleFile})");
                await examplesWriter.WriteLineAsync($"#### {description}");
                await examplesWriter.WriteLineAsync("");
                await examplesWriter.WriteLineAsync($"[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../{vars[SourceKey].Replace('\\', '/')})");
                await examplesWriter.WriteLineAsync("");
                var header = vars[HeaderKey];
                if (!string.IsNullOrWhiteSpace(header))
                {
                    await examplesWriter.WriteLineAsync(header);
                    await examplesWriter.WriteLineAsync("");
                }

                await examplesWriter.WriteLineAsync("");
                await examplesWriter.WriteLineAsync("```c#");
                await examplesWriter.WriteLineAsync(vars[BodyKey]);
                await examplesWriter.WriteLineAsync("```");
                await examplesWriter.WriteLineAsync("");

                var footer = vars[FooterKey];
                if (!string.IsNullOrWhiteSpace(footer))
                {
                    await examplesWriter.WriteLineAsync(footer);
                    await examplesWriter.WriteLineAsync("");
                }

                var exampleName = Path.GetFileNameWithoutExtension(vars[SourceKey]);
                
                await AddExample(logsDirectory, $"Pure.DI.UsageTests.*.{exampleName}.*.g.cs", examplesWriter);
                await examplesWriter.WriteLineAsync("");
                
                await AddClassDiagram(logsDirectory, exampleName, examplesWriter);
                await examplesWriter.WriteLineAsync("");
                
                await examplesWriter.FlushAsync();
            }
        }
    }

    private static async Task AddClassDiagram(string logsDirectory, string exampleName, TextWriter examplesWriter)
    {
        var classDiagramFile = Path.Combine(logsDirectory, exampleName + ".Mermaid");
        if (File.Exists(classDiagramFile))
        {
            await examplesWriter.WriteLineAsync("Class diagram:");
            await examplesWriter.WriteLineAsync("");
            await examplesWriter.WriteLineAsync("```mermaid");
            var classDiagram = await File.ReadAllTextAsync(classDiagramFile);
            await examplesWriter.WriteLineAsync(classDiagram);
            await examplesWriter.WriteLineAsync("```");
        }
    }

    private static async Task AddExample(string logsDirectory, string exampleSearchPattern, TextWriter examplesWriter)
    {
        var salt = $"M{DateTime.Now.Month:00}D{DateTime.Now.Day:00}di";
        foreach (var generatedCodeFile in Directory.GetFiles(Path.Combine(logsDirectory, "Pure.DI", "Pure.DI.SourceGenerator"), exampleSearchPattern).OrderBy(i => i))
        {
            var ns = string.Join('.', Path.GetFileName(generatedCodeFile).Split('.').Reverse().Skip(3).Reverse()) + ".";
            await examplesWriter.WriteLineAsync("The following partial class will be generated:");
            await examplesWriter.WriteLineAsync("");
            await examplesWriter.WriteLineAsync("```c#");
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
                    .Where(i =>
                    {
                        var line = i.TrimStart();
                        return !(
                            line.StartsWith("///")
                            || line.StartsWith("#if ")
                            || line.StartsWith("[global::System.Diagnostics.")
                            || line.StartsWith("#endif")
                            || line.StartsWith("#region")
                            || line.StartsWith("#endregion"));
                    })
                    .Select(i => i.Length > 1 ? i[1..] : i)
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
                        .Replace(salt, "")));
            await examplesWriter.WriteLineAsync(generatedCode);
            await examplesWriter.WriteLineAsync("```");
        }
    }

    private static async Task AddBenchmarks(string logsDirectory, TextWriter readmeWriter)
    {
        var benchmarksReportFiles = Directory.EnumerateFiles(logsDirectory, "*.html").ToArray();
        if (benchmarksReportFiles.Length != 0)
        {
            await readmeWriter.WriteLineAsync("");
            await readmeWriter.WriteLineAsync("## Benchmarks");
            await readmeWriter.WriteLineAsync("");
            var files = benchmarksReportFiles.OrderBy(i => i).ToArray();
            for (var fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                var benchmarksReportFile = files[fileIndex];
                var reportName = new string(Path.GetFileNameWithoutExtension(benchmarksReportFile).SkipWhile(ch => ch != ' ').Skip(1).ToArray());
                Info($"Processing benchmarks \"{reportName}\"");
                var lines = await File.ReadAllLinesAsync(benchmarksReportFile);
                await readmeWriter.WriteLineAsync("<details>");
                await readmeWriter.WriteLineAsync($"<summary>{reportName}</summary>");
                await readmeWriter.WriteLineAsync("");
                var contentLines = lines
                    .SkipWhile(i => !i.Contains("<table>"))
                    .TakeWhile(i => !i.Contains("</body>"))
                    .Where(i => !i.Contains("<td>NA</td>"));

                foreach (var contentLine in contentLines)
                {
                    await readmeWriter.WriteLineAsync(contentLine.Replace('?', ' '));
                }

                await readmeWriter.WriteLineAsync("");
                await readmeWriter.WriteLineAsync($"[{reportName} details]({ReadmeDir}/{reportName}Details.md)");
                await readmeWriter.WriteLineAsync("");
                await readmeWriter.WriteLineAsync("</details>");
                await readmeWriter.WriteLineAsync("");

                if (fileIndex == files.Length - 1)
                {
                    await readmeWriter.WriteLineAsync("<details>");
                    await readmeWriter.WriteLineAsync("<summary>Benchmarks environment</summary>");
                    await readmeWriter.WriteLineAsync("");

                    var headerLines = lines
                        .SkipWhile(i => !i.Contains("<pre><code>"))
                        .TakeWhile(i => !i.Contains("<pre><code></code></pre>"));

                    foreach (var headerLine in headerLines)
                    {
                        await readmeWriter.WriteLineAsync(headerLine);
                    }

                    await readmeWriter.WriteLineAsync("");
                    await readmeWriter.WriteLineAsync("</details>");
                }
            }
        }

        var benchmarks = new (string name, string description, string classDiagram)[]
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
            await classDiagramWriter.WriteLineAsync("");
            await classDiagramWriter.WriteLineAsync(description);
            await classDiagramWriter.WriteLineAsync("");
            await classDiagramWriter.WriteLineAsync("### Class diagram");
            await classDiagramWriter.WriteLineAsync("```mermaid");
            await classDiagramWriter.WriteLineAsync(classDiagram);
            await classDiagramWriter.WriteLineAsync("```");

            await classDiagramWriter.WriteLineAsync("");
            await classDiagramWriter.WriteLineAsync("### Generated code");
            await classDiagramWriter.WriteLineAsync("");
            await AddExample(logsDirectory, $"Pure.DI.Benchmarks.Benchmarks.{name}.g.cs", classDiagramWriter);
        }
    }

    private static IEnumerable<char> FormatTitle(string title)
    {
        var isFirst = true;
        foreach (var ch in title)
        {
            if (!isFirst && char.IsUpper(ch))
            {
                yield return ' ';
                yield return ch;
            }
            else
            {
                yield return ch;
            }

            isFirst = false;
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

    private enum Part
    {
        Comment,
        Body
    }
}