// ReSharper disable HeapView.ImplicitCapture
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeMadeStatic.Global
namespace Build.Tools;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
internal class Paths
{
    public string SolutionDirectory =>
        Path.GetDirectoryName(TryFindFile(Environment.CurrentDirectory, "Pure.DI.sln"))
        ?? Environment.CurrentDirectory;

    public string TempDirectory =>
        Path.Combine(Path.GetTempPath(), "Pure.DI", $"{Guid.NewGuid().ToString()[..4]}");

    private static string? TryFindFile(string? path, string searchPattern)
    {
        string? target = default;
        while (path != default && target == default)
        {
            target = Directory.EnumerateFileSystemEntries(path, searchPattern).FirstOrDefault();
            path = Path.GetDirectoryName(path);
        }

        return target;
    }
}