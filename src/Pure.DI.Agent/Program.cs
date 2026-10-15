using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

const string startMarker = "<!-- Pure.DI agent instructions: begin -->";
const string endMarker = "<!-- Pure.DI agent instructions: end -->";
const string manifestName = ".pure-di-agent.json";

try
{
    if (args.Length == 0 || args[0] is not ("init" or "remove"))
    {
        Console.Error.WriteLine("Usage: dotnet puredi-agent init|remove [--root DIRECTORY] [--project FILE.csproj] [--dry-run]");
        return 2;
    }

    var command = args[0];
    var root = Directory.GetCurrentDirectory();
    string? project = null;
    var dryRun = false;
    for (var i = 1; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--root" when i + 1 < args.Length:
                root = Path.GetFullPath(args[++i]);
                break;
            case "--project" when i + 1 < args.Length:
                project = Path.GetFullPath(args[++i]);
                break;
            case "--dry-run":
                dryRun = true;
                break;
            default:
                throw new ArgumentException($"Unknown or incomplete option: {args[i]}");
        }
    }

    if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
    var manifestPath = SafePath(manifestName);
    var manifest = File.Exists(manifestPath)
        ? JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath)) ?? new Manifest()
        : new Manifest();

    if (command == "init")
    {
        var projects = project is null ? FindProjects(root).ToArray() : [project];
        if (projects.Length == 0 || projects.All(p => !ReferencesPureDI(p)))
            throw new InvalidOperationException("No project referencing Pure.DI was found. Add the Pure.DI package first, or pass --project.");
        if (project is not null && !IsWithin(root, project))
            throw new InvalidOperationException("The selected project must be inside --root.");

        foreach (var selectedProject in projects.Where(ReferencesPureDI))
            Console.WriteLine($"Pure.DI project: {Path.GetRelativePath(root, selectedProject)} (version: {GetDeclaredVersion(selectedProject) ?? "managed by MSBuild"})");
        var assets = ReadAssets();
        foreach (var (relative, content) in assets.Where(x => x.Key.StartsWith("skills/", StringComparison.Ordinal)))
        {
            var suffix = relative["skills/".Length..];
            CheckFile($".agents/skills/{suffix}", content);
            CheckFile($".claude/skills/{suffix}", content);
        }
        CheckBlock("AGENTS.md");
        CheckBlock("CLAUDE.md");
        foreach (var (relative, content) in assets.Where(x => x.Key.StartsWith("skills/", StringComparison.Ordinal)))
        {
            var suffix = relative["skills/".Length..];
            InstallFile($".agents/skills/{suffix}", content);
            InstallFile($".claude/skills/{suffix}", content);
        }

        InstallBlock("AGENTS.md", assets["AGENTS.md"]);
        var claudePath = Path.Combine(root, "CLAUDE.md");
        if (!File.Exists(claudePath) || !File.ReadAllText(claudePath).Contains("@AGENTS.md", StringComparison.Ordinal))
            InstallBlock("CLAUDE.md", assets["CLAUDE.md"]);

        if (!dryRun)
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
    }
    else
    {
        if (!File.Exists(manifestPath))
        {
            var hasManagedInstructions = new[] { "AGENTS.md", "CLAUDE.md" }
                .Select(SafePath)
                .Any(path => File.Exists(path) && File.ReadAllText(path).Contains(startMarker, StringComparison.Ordinal));
            if (!hasManagedInstructions)
                throw new InvalidOperationException("No Pure.DI agent installation was found at this root.");
            foreach (var (relative, content) in ReadAssets().Where(x => x.Key.StartsWith("skills/", StringComparison.Ordinal)))
            {
                var suffix = relative["skills/".Length..];
                var hash = Hash(new UTF8Encoding(false).GetBytes(content));
                manifest.Files[$".agents/skills/{suffix}"] = hash;
                manifest.Files[$".claude/skills/{suffix}"] = hash;
            }
        }
        var remaining = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (relative, hash) in manifest.Files.ToArray())
        {
            var path = SafePath(relative);
            if (!File.Exists(path)) continue;
            if (Hash(File.ReadAllBytes(path)) != hash)
            {
                Console.WriteLine($"Keeping modified file: {relative}");
                remaining[relative] = hash;
                continue;
            }
            Console.WriteLine($"Remove {relative}");
            if (!dryRun) File.Delete(path);
        }
        RemoveBlock("AGENTS.md");
        RemoveBlock("CLAUDE.md");
        if (!dryRun)
        {
            if (remaining.Count == 0)
            {
                if (File.Exists(manifestPath)) File.Delete(manifestPath);
            }
            else
            {
                manifest.Files = remaining;
                File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
            }
        }
    }

    return 0;

    void CheckFile(string relative, string content)
    {
        var path = SafePath(relative);
        if (!File.Exists(path)) return;
        var existing = File.ReadAllBytes(path);
        if (existing.AsSpan().SequenceEqual(new UTF8Encoding(false).GetBytes(content))) return;
        if (!manifest.Files.TryGetValue(relative, out var oldHash) || Hash(existing) != oldHash)
            throw new InvalidOperationException($"Refusing to overwrite an unmanaged or modified file: {relative}");
    }

    void CheckBlock(string relative)
    {
        var path = SafePath(relative);
        if (!File.Exists(path)) return;
        var existing = File.ReadAllText(path);
        var start = existing.IndexOf(startMarker, StringComparison.Ordinal);
        var end = existing.IndexOf(endMarker, StringComparison.Ordinal);
        if ((start >= 0) != (end >= 0) || end >= 0 && end < start)
            throw new InvalidOperationException($"Malformed Pure.DI section in {relative}");
    }

    void InstallFile(string relative, string content)
    {
        var path = SafePath(relative);
        var bytes = new UTF8Encoding(false).GetBytes(content);
        if (File.Exists(path))
        {
            var old = File.ReadAllBytes(path);
            if (old.AsSpan().SequenceEqual(bytes))
            {
                manifest.Files[relative] = Hash(bytes);
                return;
            }
            if (!manifest.Files.TryGetValue(relative, out var oldHash) || Hash(old) != oldHash)
                throw new InvalidOperationException($"Refusing to overwrite an unmanaged or modified file: {relative}");
        }
        Console.WriteLine($"Install {relative}");
        if (!dryRun)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, bytes);
        }
        manifest.Files[relative] = Hash(bytes);
    }

    void InstallBlock(string relative, string block)
    {
        var path = SafePath(relative);
        var existing = File.Exists(path) ? File.ReadAllText(path) : "";
        var start = existing.IndexOf(startMarker, StringComparison.Ordinal);
        var end = existing.IndexOf(endMarker, StringComparison.Ordinal);
        if ((start >= 0) != (end >= 0) || end >= 0 && end < start)
            throw new InvalidOperationException($"Malformed Pure.DI section in {relative}");
        var newline = existing.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var normalizedBlock = block.TrimEnd().Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", newline, StringComparison.Ordinal);
        var updated = start >= 0
            ? existing[..start] + normalizedBlock + existing[(end + endMarker.Length)..]
            : existing + (existing.Length == 0 ? "" : (existing.EndsWith('\n') ? newline : newline + newline)) + normalizedBlock + newline;
        if (updated == existing) return;
        Console.WriteLine($"Update {relative}");
        if (!dryRun) File.WriteAllText(path, updated, new UTF8Encoding(false));
    }

    void RemoveBlock(string relative)
    {
        var path = SafePath(relative);
        if (!File.Exists(path)) return;
        var existing = File.ReadAllText(path);
        var start = existing.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0) return;
        var end = existing.IndexOf(endMarker, start, StringComparison.Ordinal);
        if (end < 0) throw new InvalidOperationException($"Malformed Pure.DI section in {relative}");
        var updated = (existing[..start].TrimEnd() + existing[(end + endMarker.Length)..]).Trim();
        Console.WriteLine($"Update {relative}");
        if (!dryRun)
        {
            if (updated.Length == 0) File.Delete(path);
            else File.WriteAllText(path, updated + "\n", new UTF8Encoding(false));
        }
    }

    string SafePath(string relative)
    {
        var path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsWithin(root, path)) throw new InvalidOperationException($"Path escapes root: {relative}");
        var current = root;
        foreach (var segment in relative.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException($"Refusing to follow a link: {current}");
        }
        return path;
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static IEnumerable<string> FindProjects(string root)
{
    var dirs = new Stack<string>();
    dirs.Push(root);
    while (dirs.TryPop(out var dir))
    {
        foreach (var file in Directory.EnumerateFiles(dir, "*.csproj")) yield return file;
        foreach (var child in Directory.EnumerateDirectories(dir))
            if (Path.GetFileName(child) is not ("bin" or "obj" or ".git" or ".vs" or "node_modules" or ".packages") &&
                (File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) dirs.Push(child);
    }
}

static bool ReferencesPureDI(string project)
{
    if (!File.Exists(project)) return false;
    var document = XDocument.Load(project);
    return document.Descendants().Any(x => x.Name.LocalName == "PackageReference" &&
        (string?)x.Attribute("Include") is "Pure.DI" or "Pure.DI.MS");
}

static string? GetDeclaredVersion(string project)
{
    var package = XDocument.Load(project).Descendants().FirstOrDefault(x => x.Name.LocalName == "PackageReference" &&
        (string?)x.Attribute("Include") is "Pure.DI" or "Pure.DI.MS");
    return (string?)package?.Attribute("Version") ?? package?.Elements().FirstOrDefault(x => x.Name.LocalName == "Version")?.Value;
}

static Dictionary<string, string> ReadAssets()
{
    var assembly = Assembly.GetExecutingAssembly();
    var assets = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("agent/", StringComparison.Ordinal)))
    {
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        assets[name["agent/".Length..].Replace('\\', '/')] = reader.ReadToEnd();
    }
    if (!assets.ContainsKey("AGENTS.md") || !assets.ContainsKey("skills/pure-di/SKILL.md"))
        throw new InvalidOperationException("The agent guidance is missing from the tool package.");
    return assets;
}

static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));

static bool IsWithin(string root, string path)
{
    var relative = Path.GetRelativePath(root, path);
    return relative != ".." && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
}

sealed class Manifest
{
    public Dictionary<string, string> Files { get; set; } = new(StringComparer.Ordinal);
}
