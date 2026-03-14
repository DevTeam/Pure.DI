// ReSharper disable ClassNeverInstantiated.Global

using System.Text.Json;

namespace Build.Core.Targets;

class ReleasesTarget(
    Settings settings,
    Commands commands,
    Env env)
    : IInitializable, ITarget<int>
{
    private const string GitHubRepoUrl = "https://api.github.com/repos/DevTeam/Pure.DI/releases";

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Get release information", "releases", "rel");

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Pure.DI-Build-Script");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        var response = await httpClient.GetAsync(GitHubRepoUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var releases = JsonSerializer.Deserialize<JsonElement>(jsonContent);

        if (releases.ValueKind != JsonValueKind.Array || !releases.EnumerateArray().Any())
        {
            Warning("No releases found.");
            return 0;
        }

        var filteredReleases = releases.EnumerateArray().ToList();
        DateTime? fromDate;
        if (!string.IsNullOrWhiteSpace(settings.ReleasesFromDate))
        {
            if (DateTime.TryParse(settings.ReleasesFromDate, out var parsedDate))
            {
                fromDate = parsedDate;
                filteredReleases = filteredReleases
                    .Where(release =>
                    {
                        var publishedAt = release.TryGetProperty("published_at", out var dateProp) ? dateProp.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(publishedAt) && DateTime.TryParse(publishedAt, out var publishDate))
                        {
                            return publishDate >= fromDate;
                        }

                        return false;
                    })
                    .ToList();

                WriteLine($"Filtering releases from: {fromDate:yyyy-MM-dd}", Color.Details);
            }
            else
            {
                Warning($"Invalid date format for 'releasesFromDate': {settings.ReleasesFromDate}. Expected format: yyyy-MM-dd");
            }
        }

        if (!filteredReleases.Any())
        {
            Warning("No releases found matching the criteria.");
            return 0;
        }

        var solutionDirectory = env.GetPath(PathType.SolutionDirectory);
        var releasesFile = Path.Combine(solutionDirectory, ".logs", "releases.json");
        Directory.CreateDirectory(Path.GetDirectoryName(releasesFile)!);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var formattedJson = JsonSerializer.Serialize(filteredReleases, options);
        await File.WriteAllTextAsync(releasesFile, formattedJson, cancellationToken);
        WriteLine($"Releases information saved to: {releasesFile}", Color.Success);

        var latestRelease = filteredReleases.FirstOrDefault();
        if (latestRelease.ValueKind != JsonValueKind.Null)
        {
            var tagName = latestRelease.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : "N/A";
            var name = latestRelease.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "N/A";
            var htmlUrl = latestRelease.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : "N/A";
            var publishedAt = latestRelease.TryGetProperty("published_at", out var dateProp) ? dateProp.GetString() : "N/A";

            WriteLine($"\nLatest Release:", Color.Details);
            WriteLine($"  Version: {tagName}", Color.Details);
            WriteLine($"  Name: {name}", Color.Details);
            WriteLine($"  Published: {publishedAt}", Color.Details);
            WriteLine($"  URL: {htmlUrl}", Color.Details);
        }

        WriteLine($"\nTotal releases: {filteredReleases.Count}", Color.Details);

        return 0;
    }
}
