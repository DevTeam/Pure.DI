// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global

namespace Pure.DI.Core;

interface IGeneratorOptions
{
    ParseOptions ParseOptions { get; }

    AnalyzerConfigOptions GlobalOptions { get; }

    AnalyzerConfigOptions GetOptions(SyntaxTree tree);
}