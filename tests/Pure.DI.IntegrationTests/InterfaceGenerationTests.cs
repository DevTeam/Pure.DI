namespace Pure.DI.IntegrationTests;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public class InterfaceGenerationTests
{
    [Fact]
    public void ShouldGenerateAnInterfaceFromAnnotatedClass()
    {
        var generated = GenerateInterfaceSource("""
            using Pure.DI;

            namespace Demo;

            public partial interface IService;

            [GenerateInterface]
            public partial class Service
            {
                public string Name { get; set; } = string.Empty;

                public event EventHandler? Changed;

                [IgnoreInterface]
                public void Hidden() { }

                public string GetText<T>(string? value)
                    where T : class
                    => value ?? string.Empty;
            }
            """);

        generated.ShouldContain("public partial interface IService");
        generated.ShouldContain("string Name { get; set; }");
        generated.ShouldContain("Changed");
        generated.ShouldContain("GetText<T>");
        generated.ShouldNotContain("Hidden");
    }

    [Fact]
    public async Task ShouldUseGeneratedInterfaceWithPureDi()
    {
        var code = """
            using Pure.DI;

            namespace Demo;

            public partial interface IService;

            [GenerateInterface]
            public partial class Service : IService
            {
                public string Message => "ok";
            }

            public partial class Consumer(IService service)
            {
                public string Message { get; } = service.Message;
            }

            partial class Setup
            {
                private static void Configure() =>
                    DI.Setup(nameof(Composition))
                        .Bind<IService>().To<Service>()
                        .Root<Consumer>(nameof(Consumer));
            }

            public class Program
            {
                public static void Main()
                {
                    var composition = new Composition();
                    var consumer = composition.Consumer;
                    System.Console.WriteLine(consumer.Message);
                }
            }
            """;

        var interfaceGenerator = new InterfaceGenerator();
        var generatedInterface = GenerateInterfaceSource(code);
        var result = await interfaceGenerator.Api
            .Select(source => source.SourceText.ToString())
            .Append(code)
            .Append(generatedInterface)
            .RunAsync(new Options(LanguageVersion.CSharp12, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldContain("ok");
    }

    private static string GenerateInterfaceSource(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            "InterfaceGenerationTests",
            [syntaxTree],
            references,
            new(OutputKind.DynamicallyLinkedLibrary));

        var sourceGenerator = new SourceGenerator();
        CSharpGeneratorDriver.Create(sourceGenerator).RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        return outputCompilation.SyntaxTrees
            .Select(tree => tree.ToString())
            .FirstOrDefault(text => text.Contains("[global::System.CodeDom.Compiler.GeneratedCode(\"Pure.DI\"" ) && text.Contains("partial interface IService"))
            ?? throw new InvalidOperationException(string.Join(Environment.NewLine + "---" + Environment.NewLine,
                outputCompilation.SyntaxTrees.Select(tree => tree.ToString())));
    }
}
