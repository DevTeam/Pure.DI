// ReSharper disable UnusedMember.Global
namespace Pure.DI;

using InterfaceGeneration;

public interface IInterfaceGenerator
{
    bool HasGenerateInterfaceAttribute(ClassDeclarationSyntax classSyntax);

    InterfaceGenerationOutput Build(GeneratorSyntaxContext syntaxContext, CancellationToken cancellationToken);

    void Generate(SourceProductionContext context, InterfaceGenerationOutput output);
}
