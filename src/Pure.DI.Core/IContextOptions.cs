// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI;

public interface IContextOptions
{
    ParseOptions ParseOptions { get; }

    AnalyzerConfigOptions GlobalOptions { get; }

    AnalyzerConfigOptions GetOptions(SyntaxTree tree);
}