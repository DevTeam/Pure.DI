// ReSharper disable MemberCanBeMadeStatic.Global

namespace Build.Tools;

using System.IO.Compression;

public class Packages
{
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public async Task<string> MergeAsync(IAsyncEnumerable<string> mergingPackages, string targetPackage, CancellationToken cancellationToken)
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

        await using var outStream = File.Create(targetPackage);
        using var outArchive = new ZipArchive(outStream, ZipArchiveMode.Create);
        var buffer = new byte[4096];
        var paths = new HashSet<string>();
        await foreach (var package in mergingPackages.WithCancellation(cancellationToken))
        {
            Info($"Processing \"{package}\".");
            await using var inStream = File.OpenRead(package);
            using var inArchive = new ZipArchive(inStream, ZipArchiveMode.Read);
            foreach (var entry in inArchive.Entries)
            {
                if (entry.Length <= 0 || !paths.Add(entry.FullName))
                {
                    WriteLine($"{entry.FullName,-100} - skipped", Color.Details);
                    continue;
                }

                await using var prevStream = entry.Open();
                var newEntry = outArchive.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                await using var newStream = newEntry.Open();
                int size;
                do
                {
                    size = await prevStream.ReadAsync(buffer, cancellationToken);
                    if (size > 0)
                    {
                        await newStream.WriteAsync(buffer.AsMemory(0, size), cancellationToken);
                    }
                } while (size > 0);

                await newStream.FlushAsync(cancellationToken);
                WriteLine($"{entry.FullName,-100} - merged", Color.Details);
            }
        }

        return targetPackage;
    }
}