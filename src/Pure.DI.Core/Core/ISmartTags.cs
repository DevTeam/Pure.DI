namespace Pure.DI.Core;

interface ISmartTags
{
    object Register(SmartTagKind kind, string name);

    IReadOnlyCollection<SmartTag> Get(SmartTagKind kind);
}