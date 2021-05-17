// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.IO;

    internal class FileSystem : IFileSystem
    {
        public DirectoryInfo CreateDirectory(string path) => 
            Directory.CreateDirectory(path);

        public void WriteFile(string path, string contents) =>
            File.WriteAllText(path, contents);

        public void AppendFile(string path, IEnumerable<string> contents) =>
            File.AppendAllLines(path, contents);
    }
}