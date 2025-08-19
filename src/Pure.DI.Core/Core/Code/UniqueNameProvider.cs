namespace Pure.DI.Core.Code;

using System.Collections.Concurrent;

class UniqueNameProvider: IUniqueNameProvider
{
    private readonly ConcurrentDictionary<string, int> _names = new();

    public string GetUniqueName(string baseName)
    {
        var id = _names.AddOrUpdate(baseName, 0, (_, id) => id + 1);
        return id == 0 ? baseName : $"{baseName}{id}";
    }
}