// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.IO;

internal class FileSystem : IFileSystem
{
    private readonly object _lockObject = new();

    public DirectoryInfo CreateDirectory(string path) =>
        Directory.CreateDirectory(path);

    public void WriteFile(string path, string contents)
    {
        lock (_lockObject)
        {
            File.WriteAllText(path, contents);
        }
    }

    public void AppendFile(string path, IEnumerable<string> contents)
    {
        lock (_lockObject)
        {
            File.AppendAllLines(path, contents);
        }
    }
}