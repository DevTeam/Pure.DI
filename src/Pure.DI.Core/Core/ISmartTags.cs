namespace Pure.DI.Core;

interface ISmartTags
{
    object Register(string name);

    IReadOnlyCollection<SmartTag> GetAll();
}