// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedType.Global
namespace Pure.DI.MSBuild.Tasks
{
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    
    public class Merge: Task
    {
        [Required]
        public string Path { get; set; } = string.Empty;
        
        [Required]
        public string Version { get; set; } = string.Empty;

        public override bool Execute()
        {
            var packageName = $"Pure.DI.{Version}.nupkg";
            var target = System.IO.Path.Combine(Path, packageName);
            Log.LogMessage(MessageImportance.High, $"Merge NuGet packages at path \"{Path}\" to \"{target}\".");
            if (File.Exists(target))
            {
                File.Delete(target);
            }

            using var outStream = File.Create(target);
            using var outArchive = new ZipArchive(outStream, ZipArchiveMode.Create);
            var packages =
                from roslynDir in Directory.GetDirectories(Path, "roslyn*")
                let packageFileName = System.IO.Path.GetFullPath(System.IO.Path.Combine( roslynDir, packageName))
                where File.Exists(packageFileName)
                select packageFileName;

            var buffer = new byte[4096];
            var paths = new HashSet<string>();
            foreach (var package in packages)
            {
                Log.LogMessage(MessageImportance.High, $"  Processing \"{package}\".");
                using var inStream = File.OpenRead(package);
                using var inArchive = new ZipArchive(inStream, ZipArchiveMode.Read);
                foreach (var entry in inArchive.Entries)
                {
                    if (entry.Length <= 0 || !paths.Add(entry.FullName))
                    {
                        Log.LogMessage(MessageImportance.High, $"    {entry.FullName.PadRight(100)} - skipped");
                        continue;
                    }

                    using var prevStream = entry.Open();
                    var newEntry = outArchive.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                    using var newStream = newEntry.Open();
                    int size;
                    do
                    {
                        size = prevStream.Read(buffer, 0, buffer.Length);
                        if (size > 0)
                        {
                            newStream.Write(buffer, 0, size);
                        }
                    } while (size > 0);
                    newStream.Flush();
                    Log.LogMessage(MessageImportance.High, $"    {entry.FullName.PadRight(100)} - merged");
                }
            }

            return true;
        }
    }
}