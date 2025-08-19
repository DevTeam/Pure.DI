namespace Pure.DI.Core.Code;

interface IUniqueNameProvider
{
    string GetUniqueName(string baseName);
}