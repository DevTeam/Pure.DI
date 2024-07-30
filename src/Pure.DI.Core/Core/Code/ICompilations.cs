namespace Pure.DI.Core.Code;

public interface ICompilations
{
    LanguageVersion GetLanguageVersion(Compilation compilation);
}