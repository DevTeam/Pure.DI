namespace Pure.DI.UsageTests;

using System.Text;

public static class TestTools
{
    public static void SaveClassDiagram(this object composition, string? name = null)
    {
        var logDirName = Path.Combine(GetSolutionDirectory(), ".logs");
        Directory.CreateDirectory(logDirName);
        var fileName = Path.Combine(logDirName, $"{name ?? composition.GetType().FullName!.Split('.').AsEnumerable().Reverse().Skip(1).First()}.Mermaid");
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
        string? target = null;
        while (path != null && target == null)
        {
            target = Directory.EnumerateFileSystemEntries(path, searchPattern).FirstOrDefault();
            path = Path.GetDirectoryName(path);
        }

        return target;
    }
}