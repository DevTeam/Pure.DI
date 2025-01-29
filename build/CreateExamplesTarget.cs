// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable InvertIf
namespace Build;

internal class CreateExamplesTarget(
    Commands commands,
    Settings settings,
    Env env)
    : IInitializable, ITarget<IReadOnlyCollection<ExampleGroup>>
{
    private const string VisibleKey = "v";
    private const string TitleKey = "t";
    private const string PriorityKey = "p";
    public const string DescriptionKey = "d";
    public const string IntegrationTestKey = "i";
    public const string HeaderKey = "h";
    public const string FooterKey = "f";
    public const string SourceKey = "s";
    public const string BodyKey = "b";
    public const string ReferencesKey = "r";
    
    private static readonly string[] Groups =
    [
        "Basics",
        "Lifetimes",
        "BaseClassLibrary",
        "Generics",
        "Attributes",
        "Interception",
        "Hints",
        "Advanced",
        "Unity"
    ];

    private static readonly char[] Separator = ['='];
    
    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Create examples", "example", "e");

    public async Task<IReadOnlyCollection<ExampleGroup>> RunAsync(CancellationToken cancellationToken)
    {
        var solutionDirectory = env.GetPath(PathType.SolutionDirectory);
        var usageTestsProject = Path.Combine(solutionDirectory, "tests", "Pure.DI.UsageTests", "Pure.DI.UsageTests.csproj");
        var projects = new[]
        {
            usageTestsProject,
            Path.Combine(solutionDirectory, "benchmarks", "Pure.DI.Benchmarks", "Pure.DI.Benchmarks.csproj")
        };

        var msbuild = new MSBuild()
            .WithTarget("clean;rebuild")
            .WithProps(("Configuration", settings.Configuration));

        foreach (var project in projects)
        {
            await msbuild
                .WithProject(project)
                .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();
        }

        await new DotNetTest()
            .WithProject(usageTestsProject)
            .WithNoBuild(true)
            .WithConfiguration(settings.Configuration)
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();
        
        return await CreateExamplesAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<ExampleGroup>> CreateExamplesAsync(CancellationToken cancellationToken)
    {
        var solutionDir = env.GetPath(PathType.SolutionDirectory);
        var items = new List<Dictionary<string, string>>();
        var testsDir = Path.Combine(solutionDir, "tests", "Pure.DI.UsageTests");
        var files = Directory.EnumerateFiles(testsDir, "*.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(Environment.CurrentDirectory, file);
            Part? part = null;
            var vars = new Dictionary<string, string>
            {
                [VisibleKey] = "false",
                [TitleKey] = Path.GetDirectoryName(Path.GetRelativePath(testsDir, file)) ?? "",
                [PriorityKey] = string.Empty,
                [DescriptionKey] = string.Empty,
                [IntegrationTestKey] = "true",
                [HeaderKey] = string.Empty,
                [FooterKey] = string.Empty,
                [SourceKey] = relativePath,
                [ReferencesKey] = string.Empty
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
                    part = null;
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
                    part = null;
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

                    if (line.TrimStart().StartsWith("//# "))
                    {
                        localBody.Add(line.Replace("//# ", ""));
                        continue;
                    }

                    localBody.Add(line);
                }
            }
            
            if (body.Count != 0)
            {
                var references = vars[ReferencesKey].Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                vars[BodyKey] = string.Join(Environment.NewLine, references.Select(i => $"using {i};").Concat(body));
            }
        }

        var groups = Groups
            .Select((name, index) => (name, index))
            .ToDictionary(i => i.name, i => i.index);

        return items
            .Where(i => i.ContainsKey(BodyKey) && i[VisibleKey] != "False")
            .GroupBy(i => i[TitleKey])
            .OrderBy(i => groups.TryGetValue(i.Key, out var index) ? index : int.MaxValue)
            .Select(i => new ExampleGroup(
                i.Key,
                i.OrderBy(j => int.Parse(j[PriorityKey])).ThenBy(j => j[DescriptionKey]).Select(j => new Example(j)).ToList()))
            .ToList();
    }
    
    private enum Part
    {
        Comment,
        Body
    }
}