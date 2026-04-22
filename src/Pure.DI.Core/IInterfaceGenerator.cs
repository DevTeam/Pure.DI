namespace Pure.DI;

public interface IInterfaceGenerator
{
    IEnumerable<Source> Api { get; }

    bool HasGenerateInterfaceAttribute(ClassDeclarationSyntax classSyntax);

    void Generate(SourceProductionContext context, ImmutableArray<GeneratorSyntaxContext> syntaxContexts);
}