namespace Pure.DI.Core.Code;

using System.Collections.Concurrent;

class UniqueNameProvider: IUniqueNameProvider
{
    private readonly ConcurrentDictionary<string, int> _names = new();
    private readonly ConcurrentDictionary<string, bool> _uniqueNames = new();

    public string GetUniqueName(string baseName)
    {
        // The next name of a base name can be a name of another base name that ends with a number,
        // for example, the second name of "Service1" and the first name of "Service11".
        while (true)
        {
            var id = _names.AddOrUpdate(baseName, 0, (_, id) => id + 1);
            var name = id == 0 ? baseName : $"{baseName}{id}";
            if (_uniqueNames.TryAdd(name, true))
            {
                return name;
            }
        }
    }
}