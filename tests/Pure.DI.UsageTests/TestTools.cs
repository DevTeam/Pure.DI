namespace Pure.DI.UsageTests;

using System.Text;

internal static class TestTools
{
    public static void SaveClassDiagram(object composition, string name)
    {
        var dirName = Path.Combine(GetSolutionDirectory(), ".logs");
        Path.GetDirectoryName(dirName);
        var fileName = Path.Combine(dirName, $"{name}.Mermaid");
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        File.WriteAllText(fileName, composition.ToString(), Encoding.Unicode);
    }

    private static string GetSolutionDirectory()
    {
        var solutionFile = TryFindFile(Environment.CurrentDirectory, "Pure.DI.sln") ?? Environment.CurrentDirectory;
        return Path.GetDirectoryName(solutionFile) ?? Environment.CurrentDirectory;
    }

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