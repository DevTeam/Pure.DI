namespace Pure.DI
{
    using System.IO;

    internal interface IFileSystem
    {
        DirectoryInfo CreateDirectory(string path);

        void WriteFile(string path, string contents);
    }
}
