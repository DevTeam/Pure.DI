namespace Pure.DI.Core;

using System.Text.RegularExpressions;

internal sealed class Resources(ICache<string, Regex> regexCache) : IResources
{
    public IEnumerable<Resource> GetResource(string filter)
    {
        var filterRegex = regexCache.Get(filter);
        var assembly = typeof(Resources).Assembly;
        return assembly
            .GetManifestResourceNames()
            .Where(name => filterRegex.IsMatch(name))
            .Select(name => new Resource(name, assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Cannot read the resource {name}.")));
    }
}