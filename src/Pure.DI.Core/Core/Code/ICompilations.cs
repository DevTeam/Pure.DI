namespace Pure.DI.Core.Code;

internal interface ICompilations
{
    LanguageVersion GetLanguageVersion(Compilation compilation);
}