// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class Compilations : ICompilations
{
    public LanguageVersion GetLanguageVersion(Compilation compilation) =>
        compilation is CSharpCompilation sharpCompilation
            ? sharpCompilation.LanguageVersion
            : LanguageVersion.Default;
}