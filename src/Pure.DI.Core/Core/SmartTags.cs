// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

sealed class SmartTags : ISmartTags
{
    private readonly HashSet<SmartTag> _tags = [];

    public object Register(SmartTagKind kind, string name)
    {
        if (SyntaxFacts.IsValidIdentifier(name)
            && name != nameof(Tag.Type)
            && name != nameof(Tag.Unique))
        {
            lock (_tags)
            {
                _tags.Add(new SmartTag(kind, name));
                return name;
            }
        }

        return name;
    }

    public IReadOnlyCollection<SmartTag> Get(SmartTagKind kind)
    {
        lock (_tags)
        {
            return _tags.Where(i => i.Kind == kind).ToList();
        }
    }
}