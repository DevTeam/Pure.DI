// ReSharper disable UnusedMethodReturnValue.Global
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.IO;

    internal interface IFileSystem
    {
        DirectoryInfo CreateDirectory(string path);

        void WriteFile(string path, string contents);

        void AppendFile(string path, IEnumerable<string> contents);
    }
}
