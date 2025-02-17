// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeMadeStatic.Global

namespace Build.Tools;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
class FileSystem
{
    private readonly Lazy<HashSet<char>> _invalidFileNameChars = new(() => Path.GetInvalidFileNameChars().ToHashSet());

    public ISet<char> InvalidFileNameChars => _invalidFileNameChars.Value;

    public bool IsFileExist(string file) => File.Exists(file);

    public bool IsDirectoryExist(string directory) => Directory.Exists(directory);

    public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFileSystemEntries(path, searchPattern, searchOption);

    public string GetTempFileName() => Path.GetTempFileName();

    public Stream CreateFile(string file) => File.Create(file);

    public StreamWriter CreateText(string file) => File.CreateText(file);

    public Stream OpenRead(string file) => File.OpenRead(file);

    public TextReader OpenText(string file) => File.OpenText(file);

    public void DeleteFile(string file) => File.Delete(file);

    public void DeleteDirectory(string directory, bool recursive) => Directory.Delete(directory, recursive);

    public void MoveDirectory(string sourceDirectory, string destinationDirectory) => Directory.Move(sourceDirectory, destinationDirectory);

    public void CreateDirectory(string directory) => Directory.CreateDirectory(directory);

    public void CopyFile(string sourceFile, string destinationFile, bool overwrite) => File.Copy(sourceFile, destinationFile, overwrite);

    public DirectoryInfo? GetParent(string path) => Directory.GetParent(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);
}