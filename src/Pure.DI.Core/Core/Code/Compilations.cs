// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class Compilations : ICompilations
{
    public LanguageVersion GetLanguageVersion(Compilation compilation) =>
        compilation is CSharpCompilation sharpCompilation
            ? sharpCompilation.LanguageVersion
            : LanguageVersion.Default;
}