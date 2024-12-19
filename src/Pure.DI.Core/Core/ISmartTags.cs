namespace Pure.DI.Core;

internal interface ISmartTags
{
    object Register(string name);

    IReadOnlyCollection<SmartTag> GetAll();
}