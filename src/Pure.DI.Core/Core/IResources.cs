namespace Pure.DI.Core;

internal interface IResources
{
    IEnumerable<Resource> GetResource(string filter);
}