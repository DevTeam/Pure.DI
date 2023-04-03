namespace Build;

using System.CommandLine.Invocation;

public class ReadmeTarget : ITarget
{
    private const string ReadmeDir = "readme";
    private const string ExamplesReadmeFile = "Examples.md";
    private const string ReadmeTemplateFile = "ReadmeTemplate.md";
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
        "BaseClassLibrary",
        "Interception"
    };

    public async Task RunAsync(InvocationContext ctx)
    {
        var solutionDirectory = Tools.GetSolutionDirectory();
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
            .ToDictionary(i => i.name , i=> i.index);

        var samples = items
            .Where(i => i.ContainsKey(BodyKey) && i[VisibleKey] != "False")
            .GroupBy(i => i[TitleKey])
            .OrderBy(i => groups.TryGetValue(i.Key, out var index) ? index : int.MaxValue)
            .Select(i => (GroupName: i.Key, SampleItems: i.OrderBy(j => int.Parse(j[PriorityKey])).ThenBy(j => j[DescriptionKey]).ToArray()));
        
        await using var readmeWriter = File.CreateText(ReadmeFile);
        foreach (var line in await File.ReadAllLinesAsync(Path.Combine(ReadmeDir, ReadmeTemplateFile)))
        {
            await readmeWriter.WriteLineAsync(line);
        }
        
        await using var examplesWriter = File.CreateText(Path.Combine(ReadmeDir, ExamplesReadmeFile));
        await readmeWriter.WriteLineAsync("## Examples");
        foreach (var (groupName, sampleItems) in samples)
        {
            await readmeWriter.WriteLineAsync($"### {new string(FormatTitle(groupName).ToArray())}");
            foreach (var vars in sampleItems)
            {
                var description = vars[DescriptionKey];
                await readmeWriter.WriteLineAsync($"- [{description}]({ReadmeDir}/{ExamplesReadmeFile}{CreateRef(description)})");
                await examplesWriter.WriteLineAsync($"#### {description}");
                await examplesWriter.WriteLineAsync("");
                await examplesWriter.WriteLineAsync($"[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)]({vars[SourceKey]})");
                var header = vars[HeaderKey];
                if (!string.IsNullOrWhiteSpace(header))
                {
                    await examplesWriter.WriteLineAsync("");
                    await examplesWriter.WriteLineAsync(header);
                }

                await examplesWriter.WriteLineAsync("");
                await examplesWriter.WriteLineAsync("``` CSharp");
                await examplesWriter.WriteLineAsync(vars[BodyKey]);
                await examplesWriter.WriteLineAsync("```");

                var classDiagramFile = Path.Combine(solutionDirectory, ".logs", Path.GetFileNameWithoutExtension(vars[SourceKey]) + ".Mermaid");
                if (File.Exists(classDiagramFile))
                {
                    await examplesWriter.WriteLineAsync("");
                    await examplesWriter.WriteLineAsync("```mermaid");
                    var classDiagram = await File.ReadAllTextAsync(classDiagramFile);
                    await examplesWriter.WriteLineAsync(classDiagram);
                    await examplesWriter.WriteLineAsync("```");
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
        
        await readmeWriter.FlushAsync();
        await examplesWriter.FlushAsync();
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