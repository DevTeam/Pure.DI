// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
namespace Build;

using System.CommandLine.Invocation;
using HostApi;
using Pure.DI;

internal class ReadmeTarget : ITarget<int>
{
    private readonly ITarget<int> _benchmarksTarget;
    private const string ReadmeDir = "readme";
    private const string ExamplesReadmeFile = "Examples.md";
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
    {
        "Basics",
        "Lifetimes",
        "Attributes",
        "BaseClassLibrary",
        "Interception",
        "Hints"
    };

    public ReadmeTarget([Tag("benchmarks")] ITarget<int> benchmarksTarget)
    {
        _benchmarksTarget = benchmarksTarget;
    }

    public async Task<int> RunAsync(InvocationContext ctx)
    {
        var solutionDirectory = Tools.GetSolutionDirectory();
        var logsDirectory = Path.Combine(solutionDirectory, ".logs");
        
        // Run benchmarks
        await _benchmarksTarget.RunAsync(ctx);
        
        // Run tests for Class Diagrams
        var testProject = Path.Combine(solutionDirectory, "tests", "Pure.DI.UsageTests", "Pure.DI.UsageTests.csproj");
        await new DotNetClean().WithProject(testProject).BuildAsync();
        var testResult = await new DotNetTest().WithProject(testProject).BuildAsync();
        Assertion.Succeed(testResult);
        
        await using var readmeWriter = File.CreateText(ReadmeFile);
        
        await AddContent(ReadmeTemplateFile, readmeWriter);
        
        await readmeWriter.WriteLineAsync("");
        
        var examples = await CreateExamples(ctx);
        await GenerateExamples(examples, readmeWriter, logsDirectory);

        await AddContent(FooterTemplateFile, readmeWriter);
        
        await readmeWriter.WriteLineAsync("");
        
        await AddBenchmarks(logsDirectory, readmeWriter);

        await readmeWriter.FlushAsync();
        return 0;
    }

    private static async Task AddContent(string sourceFile, TextWriter readmeWriter)
    {
        WriteLine($"Adding a content from \"{sourceFile}\"", Color.Details);
        foreach (var line in await File.ReadAllLinesAsync(Path.Combine(ReadmeDir, sourceFile)))
        {
            await readmeWriter.WriteLineAsync(line);
        }
    }

    private static async Task<IEnumerable<(string GroupName, Dictionary<string, string>[] SampleItems)>> CreateExamples(InvocationContext ctx)
    {
        var items = new List<Dictionary<string, string>>();
        var testsDir = Path.Combine(Tools.GetSolutionDirectory(), "tests", "Pure.DI.UsageTests");
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
            foreach (var line in await File.ReadAllLinesAsync(file, ctx.GetCancellationToken()))
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
                    if (body.Any())
                    {
                        body.Add("");
                    }

                    body.AddRange(localBody.Select(i => i.Length > offset ? i[offset..].TrimEnd() : i));
                    offset = int.MaxValue;
                    localBody.Clear();
                    part = default;
                    continue;
                }

                if (part == Part.Comment && str.StartsWith("$"))
                {
                    var parts = line[1..].Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
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

            if (body.Any())
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

    private static async Task GenerateExamples(IEnumerable<(string GroupName, Dictionary<string, string>[] SampleItems)> examples, TextWriter readmeWriter, string logsDirectory)
    {
        await readmeWriter.WriteLineAsync("## Examples");
        await readmeWriter.WriteLineAsync("");
        
        await using var examplesWriter = File.CreateText(Path.Combine(ReadmeDir, ExamplesReadmeFile));
        foreach (var (groupName, exampleItems) in examples)
        {
            var groupTitle = new string(FormatTitle(groupName).ToArray());
            WriteLine($"Processing examples group \"{groupTitle}\"", Color.Details);
            await readmeWriter.WriteLineAsync($"### {groupTitle}");
            foreach (var vars in exampleItems)
            {
                var description = vars[DescriptionKey];
                WriteLine($"  · \"{description}\"", Color.Details);
                await readmeWriter.WriteLineAsync($"- [{description}]({ReadmeDir}/{ExamplesReadmeFile}{CreateRef(description)})");
                await examplesWriter.WriteLineAsync($"#### {description}");
                await examplesWriter.WriteLineAsync("");
                await examplesWriter.WriteLineAsync($"[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../{vars[SourceKey].Replace('\\', '/')})");
                var header = vars[HeaderKey];
                if (!string.IsNullOrWhiteSpace(header))
                {
                    await examplesWriter.WriteLineAsync("");
                    await examplesWriter.WriteLineAsync(header);
                }

                await examplesWriter.WriteLineAsync("");
                await examplesWriter.WriteLineAsync("```c#");
                await examplesWriter.WriteLineAsync(vars[BodyKey]);
                await examplesWriter.WriteLineAsync("```");

                var classDiagramFile = Path.Combine(logsDirectory, Path.GetFileNameWithoutExtension(vars[SourceKey]) + ".Mermaid");
                if (File.Exists(classDiagramFile))
                {
                    await examplesWriter.WriteLineAsync("");
                    await examplesWriter.WriteLineAsync("<details open>");
                    await examplesWriter.WriteLineAsync("<summary>Class Diagram</summary>");
                    await examplesWriter.WriteLineAsync("");
                    await examplesWriter.WriteLineAsync("```mermaid");
                    var classDiagram = await File.ReadAllTextAsync(classDiagramFile);
                    await examplesWriter.WriteLineAsync(classDiagram);
                    await examplesWriter.WriteLineAsync("```");
                    await examplesWriter.WriteLineAsync("");
                    await examplesWriter.WriteLineAsync("</details>");
                    await examplesWriter.WriteLineAsync("");
                }

                var footer = vars[FooterKey];
                if (!string.IsNullOrWhiteSpace(footer))
                {
                    await examplesWriter.WriteLineAsync("");
                    await examplesWriter.WriteLineAsync(footer);
                }

                await examplesWriter.WriteLineAsync("");
            }
        }

        await examplesWriter.FlushAsync();
    }

    private static async Task AddBenchmarks(string logsDirectory, TextWriter readmeWriter)
    {
        var benchmarksReportFiles = Directory.EnumerateFiles(logsDirectory, "*.html").ToArray();
        if (benchmarksReportFiles.Any())
        {
            await readmeWriter.WriteLineAsync("");
            await readmeWriter.WriteLineAsync("## Benchmarks");
            await readmeWriter.WriteLineAsync("");
            var files = benchmarksReportFiles.OrderBy(i => i).ToArray();
            for (var fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                var benchmarksReportFile = files[fileIndex];
                var reportName = new string(Path.GetFileNameWithoutExtension(benchmarksReportFile).SkipWhile(ch => ch != ' ').Skip(1).ToArray());
                WriteLine($"Processing benchmarks \"{reportName}\"", Color.Details);
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

    private static string CreateRef(string text) =>
        "#" + text.Replace(" ", "-")
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