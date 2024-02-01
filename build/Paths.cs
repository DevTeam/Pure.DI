// ReSharper disable HeapView.ImplicitCapture
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeMadeStatic.Global
namespace Build;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
internal class Paths
{
    public string GetSolutionDirectory() => 
        Path.GetDirectoryName(TryFindFile(Environment.CurrentDirectory, "Pure.DI.sln"))
        ?? Environment.CurrentDirectory;

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