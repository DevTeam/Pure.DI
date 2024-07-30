namespace Pure.DI.Core.Code;

public class Compilations : ICompilations
{
    public LanguageVersion GetLanguageVersion(Compilation compilation) =>
        compilation is CSharpCompilation sharpCompilation
            ? sharpCompilation.LanguageVersion
            : LanguageVersion.Default;
}