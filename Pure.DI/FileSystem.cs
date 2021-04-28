namespace Pure.DI
{
    using System.IO;

    internal class FileSystem : IFileSystem
    {
        public DirectoryInfo CreateDirectory(string path) => 
            Directory.CreateDirectory(path);

        public void WriteFile(string path, string contents) =>
            File.WriteAllText(path, contents);
    }
}