// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

sealed class Compilations : ICompilations
{
    public LanguageVersion GetLanguageVersion(Compilation compilation) =>
        compilation is CSharpCompilation sharpCompilation
            ? sharpCompilation.LanguageVersion
            : LanguageVersion.Default;
}