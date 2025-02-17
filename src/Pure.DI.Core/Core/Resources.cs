// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Reflection;
using System.Text.RegularExpressions;

sealed class Resources(
    Assembly assembly,
    Func<string, Regex> regexFactory,
    ICache<string, Regex> regexCache)
    : IResources
{
    public IEnumerable<Resource> GetResource(string filter)
    {
        var filterRegex = regexCache.Get(filter, regexFactory);
        return assembly
            .GetManifestResourceNames()
            .Where(name => filterRegex.IsMatch(name))
            .Select(name => new Resource(name, assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Cannot read the resource {name}.")));
    }
}