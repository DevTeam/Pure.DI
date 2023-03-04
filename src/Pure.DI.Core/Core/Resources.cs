namespace Pure.DI.Core;

using System.Text.RegularExpressions;

internal class Resources: IResources
{
    private readonly ICache<string, Regex> _regexCache;

    public Resources(ICache<string,  Regex> regexCache) => _regexCache = regexCache;

    public IEnumerable<Resource> GetResource(string filter)
    {
        var filterRegex = _regexCache.Get(filter);
        var assembly = typeof(Resources).Assembly;
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!filterRegex.IsMatch(name))
            {
                continue;
            }

            yield return new Resource(name, assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Cannot read the resource {name}."));
        }
    }
}