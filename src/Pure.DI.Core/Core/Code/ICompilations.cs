namespace Pure.DI.Core.Code;

interface ICompilations
{
    LanguageVersion GetLanguageVersion(Compilation compilation);
}