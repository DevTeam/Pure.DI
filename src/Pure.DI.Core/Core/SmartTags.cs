// ReSharper disable InvertIf
namespace Pure.DI.Core;

internal class SmartTags : ISmartTags
{
    private readonly HashSet<SmartTag> _tags = [];
    
    public object Register(string name)
    {
        if (SyntaxFacts.IsValidIdentifier(name)
            && name != nameof(Tag.Type)
            && name != nameof(Tag.Unique))
        {
            lock (_tags)
            {
                _tags.Add(new SmartTag(name));
                return name;
            }
        }

        return name;
    }

    public IReadOnlyCollection<SmartTag> GetAll()
    {
        lock (_tags)
        {
            return _tags.ToList();
        }
    }
}