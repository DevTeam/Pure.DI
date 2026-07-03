namespace Pure.DI.IntegrationTests;

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// Verifies that every localized resource file Strings.*.resx is complete:
/// it contains exactly the same set of keys as the base Strings.resx,
/// each value is not empty, and format placeholders like {0} match the base value.
/// </summary>
[SuppressMessage("Usage", "xUnit1042:The member referenced by the MemberData attribute returns untyped data rows")]
public partial class LocalizationTests
{
    private static readonly string ResourcesDirectory = Path.Combine(GetSolutionDirectory(), "src", "Pure.DI.Core");

    public static IEnumerable<object[]> LocalizedResourceFiles =>
        Directory.EnumerateFiles(ResourcesDirectory, "Strings.*.resx")
            .Where(file => !file.EndsWith("Strings.resx", StringComparison.OrdinalIgnoreCase))
            .Select(file => new object[] { Path.GetFileName(file) });

    [Fact]
    public void ShouldHaveLocalizedResourceFiles()
    {
        LocalizedResourceFiles.ShouldNotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(LocalizedResourceFiles))]
    public void ShouldContainAllKeysFromBaseResource(string localizedFileName)
    {
        // Given
        var baseKeys = ReadResources(BaseResourceFile).Keys.ToHashSet();

        // When
        var localizedKeys = ReadResources(Path.Combine(ResourcesDirectory, localizedFileName)).Keys.ToHashSet();

        // Then
        var missingKeys = baseKeys.Except(localizedKeys).OrderBy(key => key).ToList();
        missingKeys.ShouldBeEmpty($"{localizedFileName} is missing keys: {string.Join(", ", missingKeys)}");
    }

    [Theory]
    [MemberData(nameof(LocalizedResourceFiles))]
    public void ShouldNotContainExtraKeys(string localizedFileName)
    {
        // Given
        var baseKeys = ReadResources(BaseResourceFile).Keys.ToHashSet();

        // When
        var localizedKeys = ReadResources(Path.Combine(ResourcesDirectory, localizedFileName)).Keys.ToHashSet();

        // Then
        var extraKeys = localizedKeys.Except(baseKeys).OrderBy(key => key).ToList();
        extraKeys.ShouldBeEmpty($"{localizedFileName} contains keys absent in Strings.resx: {string.Join(", ", extraKeys)}");
    }

    [Theory]
    [MemberData(nameof(LocalizedResourceFiles))]
    public void ShouldNotContainEmptyValues(string localizedFileName)
    {
        // Given

        // When
        var localizedResources = ReadResources(Path.Combine(ResourcesDirectory, localizedFileName));

        // Then
        var emptyKeys = localizedResources
            .Where(resource => string.IsNullOrWhiteSpace(resource.Value))
            .Select(resource => resource.Key)
            .OrderBy(key => key)
            .ToList();

        emptyKeys.ShouldBeEmpty($"{localizedFileName} contains empty values for keys: {string.Join(", ", emptyKeys)}");
    }

    [Theory]
    [MemberData(nameof(LocalizedResourceFiles))]
    public void ShouldPreserveFormatPlaceholders(string localizedFileName)
    {
        // Given
        var baseResources = ReadResources(BaseResourceFile);

        // When
        var localizedResources = ReadResources(Path.Combine(ResourcesDirectory, localizedFileName));

        // Then
        var brokenKeys = new List<string>();
        foreach (var resource in baseResources)
        {
            if (!localizedResources.TryGetValue(resource.Key, out var localizedValue))
            {
                continue;
            }

            var basePlaceholders = GetPlaceholders(resource.Value);
            var localizedPlaceholders = GetPlaceholders(localizedValue);
            if (!basePlaceholders.SetEquals(localizedPlaceholders))
            {
                brokenKeys.Add($"{resource.Key} (expected: [{string.Join(", ", basePlaceholders.OrderBy(placeholder => placeholder))}], actual: [{string.Join(", ", localizedPlaceholders.OrderBy(placeholder => placeholder))}])");
            }
        }

        brokenKeys.ShouldBeEmpty($"{localizedFileName} has placeholder mismatches: {string.Join("; ", brokenKeys)}");
    }

    private static string BaseResourceFile => Path.Combine(ResourcesDirectory, "Strings.resx");

    private static Dictionary<string, string> ReadResources(string file)
    {
        File.Exists(file).ShouldBeTrue($"Resource file {file} was not found.");
        return XDocument.Load(file)
            .Root!
            .Elements("data")
            .ToDictionary(
                data => data.Attribute("name")!.Value,
                data => data.Element("value")?.Value ?? "");
    }

    private static HashSet<string> GetPlaceholders(string value) =>
        PlaceholderRegex().Matches(value).Select(match => match.Value).ToHashSet();

    private static string GetSolutionDirectory()
    {
        var path = AppContext.BaseDirectory;
        while (path is not null && !File.Exists(Path.Combine(path, "Pure.DI.sln")))
        {
            path = Path.GetDirectoryName(path);
        }

        return path ?? throw new InvalidOperationException("Cannot find the solution directory containing Pure.DI.sln.");
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderRegex();
}
