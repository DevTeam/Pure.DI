// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests;

public class ScenarioMetadataTests
{
    [Fact]
    public void ShouldContainRequiredDocumentationMetadata()
    {
        var solutionDir = GetSolutionDirectory();
        var scenariosDir = Path.Combine(solutionDir, "tests", "Pure.DI.UsageTests");
        var readmeDir = Path.Combine(solutionDir, "readme");
        var scenarioFiles = Directory
            .EnumerateFiles(scenariosDir, "*Scenario.cs", SearchOption.AllDirectories)
            .OrderBy(i => i)
            .ToArray();

        scenarioFiles.Length.ShouldBeGreaterThan(0);

        var errors = new List<string>();
        foreach (var file in scenarioFiles)
        {
            var lines = File.ReadAllLines(file);
            if (!TryGetMetadata(lines, out var metadata))
            {
                errors.Add($"Metadata block not found: {file}");
                continue;
            }

            if (!metadata.Any(i => i.StartsWith("$d=", StringComparison.Ordinal)))
            {
                errors.Add($"Missing $d in {file}");
            }

            if (!metadata.Any(i => i.StartsWith("$h=", StringComparison.Ordinal)))
            {
                errors.Add($"Missing $h in {file}");
            }

            if (!metadata.Any(i => i.StartsWith("$f=", StringComparison.Ordinal)))
            {
                errors.Add($"Missing $f in {file}");
            }

            // Guard against common mojibake fragments in generated docs.
            if (metadata.Any(i => i.Contains("вњ", StringComparison.Ordinal) || i.Contains("вќ", StringComparison.Ordinal)))
            {
                errors.Add($"Suspicious encoding artifacts in {file}");
            }

            foreach (var mdLink in GetMarkdownLinks(metadata))
            {
                if (mdLink.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    mdLink.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    mdLink.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                    mdLink.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var linkPath = mdLink.Split('#')[0];
                var resolved = Path.GetFullPath(Path.Combine(readmeDir, linkPath));
                if (!File.Exists(resolved))
                {
                    errors.Add($"Broken documentation link in {file}: {mdLink} -> {resolved}");
                }
            }
        }

        errors.ShouldBeEmpty();
    }

    private static bool TryGetMetadata(string[] lines, out IReadOnlyList<string> metadata)
    {
        metadata = [];
        var start = Array.IndexOf(lines, "/*");
        var end = Array.IndexOf(lines, "*/");
        if (start < 0 || end <= start)
        {
            return false;
        }

        var block = lines[(start + 1)..end];
        if (!block.Any(i => i.StartsWith("$v=", StringComparison.Ordinal)))
        {
            return false;
        }

        metadata = block
            .Where(i => i.StartsWith("$", StringComparison.Ordinal))
            .ToArray();

        return true;
    }

    private static string GetSolutionDirectory()
    {
        var path = Environment.CurrentDirectory;
        while (path is not null)
        {
            if (Directory.EnumerateFiles(path, "Pure.DI.sln").Any())
            {
                return path;
            }

            path = Path.GetDirectoryName(path);
        }

        return Environment.CurrentDirectory;
    }

    private static IEnumerable<string> GetMarkdownLinks(IEnumerable<string> lines)
    {
        var regex = new System.Text.RegularExpressions.Regex(@"\[[^\]]+\]\(([^)]+)\)");
        foreach (var line in lines)
        {
            var matches = regex.Matches(line);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                yield return match.Groups[1].Value.Trim();
            }
        }
    }
}
