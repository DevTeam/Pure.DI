// ReSharper disable MemberCanBeMadeStatic.Global
namespace Build.Tools;

using System.IO.Compression;

[SuppressMessage("Performance", "CA1822:Пометьте члены как статические")]
public class Packages
{
    public string Merge(IEnumerable<string> mergingPackages, string targetPackage)
    {
        Info($"Creating NuGet package {targetPackage}");
        var targetDir = Path.GetDirectoryName(targetPackage);
        if (!string.IsNullOrWhiteSpace(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        if (File.Exists(targetPackage))
        {
            File.Delete(targetPackage);
        }

        using var outStream = File.Create(targetPackage);
        using var outArchive = new ZipArchive(outStream, ZipArchiveMode.Create);
        var buffer = new byte[4096];
        var paths = new HashSet<string>();
        foreach (var package in mergingPackages)
        {
            Info($"Processing \"{package}\".");
            using var inStream = File.OpenRead(package);
            using var inArchive = new ZipArchive(inStream, ZipArchiveMode.Read);
            foreach (var entry in inArchive.Entries)
            {
                if (entry.Length <= 0 || !paths.Add(entry.FullName))
                {
                    WriteLine($"{entry.FullName, -100} - skipped", Color.Details);
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
                WriteLine($"{entry.FullName, -100} - merged", Color.Details);
            }
        }

        return targetPackage;
    }
}