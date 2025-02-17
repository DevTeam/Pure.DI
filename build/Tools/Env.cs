// ReSharper disable MemberCanBeMadeStatic.Global

namespace Build.Tools;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
class Env
{
    public string GetPath(PathType pathType) =>
        pathType switch
        {
            PathType.SolutionDirectory => Path.GetDirectoryName(TryFindFile(Environment.CurrentDirectory, "Pure.DI.sln")) ?? Environment.CurrentDirectory,
            PathType.TempDirectory => Path.Combine(Path.GetTempPath(), "Pure.DI", $"{Guid.NewGuid().ToString()[..4]}"),
            PathType.BenchmarksResultDirectory => Path.Combine(GetPath(PathType.SolutionDirectory), "benchmarks", "data"),
            _ => throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null)
        };

    private static string? TryFindFile(string? path, string searchPattern)
    {
        string? target = null;
        while (path != null && target == null)
        {
            target = Directory.EnumerateFileSystemEntries(path, searchPattern).FirstOrDefault();
            path = Path.GetDirectoryName(path);
        }

        return target;
    }
}