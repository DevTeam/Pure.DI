namespace Pure.DI;

public interface IContextOptions
{
    ParseOptions ParseOptions { get; }

    AnalyzerConfigOptions GlobalOptions { get; }

    AnalyzerConfigOptions GetOptions(SyntaxTree tree);
}