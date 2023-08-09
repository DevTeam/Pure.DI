namespace Pure.DI.Core;

internal sealed class FileSystem : IFileSystem
{
    public void AppendAllText(string path, string contents) =>
        File.AppendAllText(path, contents);
    
    public void CreateDirectory(string path) =>
        Directory.CreateDirectory(path);
    
    public string? GetDirectoryName(string path) =>
        System.IO.Path.GetDirectoryName(path);
}