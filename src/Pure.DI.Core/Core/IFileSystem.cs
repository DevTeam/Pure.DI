namespace Pure.DI.Core;

internal interface IFileSystem
{
    void AppendAllText(string path, string contents);
    
    void CreateDirectory(string path);
    
    string? GetDirectoryName(string path);
}