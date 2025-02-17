namespace Pure.DI.Core;

interface IResources
{
    IEnumerable<Resource> GetResource(string filter);
}