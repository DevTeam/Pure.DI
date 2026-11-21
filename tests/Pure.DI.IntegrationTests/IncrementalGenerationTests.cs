// ReSharper disable StringLiteralTypo
namespace Pure.DI.IntegrationTests;

using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Checks that the generator skips the work after edits that cannot change the generated code,
/// and that the code it keeps or generates after an edit is the same as the code generated from scratch.
/// </summary>
public class IncrementalGenerationTests
{
    private const string SetupFile = "Setup.cs";
    private const string ServicesFile = "Services.cs";
    private const string CompositionPartFile = "CompositionPart.cs";
    private const string ProgramFile = "Program.cs";
    private const string InterfacesFile = "Interfaces.cs";

    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10);

    private static readonly Dictionary<string, string> Sources = new()
    {
        [SetupFile] = """
            using Pure.DI;

            namespace Sample;

            partial class Composition
            {
                private void Setup() =>
                    DI.Setup(nameof(Composition))
                        .Bind<IDependency>().To<Dependency>()
                        .Bind<IService>().To<Service>()
                        .Root<IService>("Service");
            }
            """,
        [ServicesFile] = """
            namespace Sample;

            interface IDependency { }

            class Dependency : IDependency
            {
                public int Calc()
                {
                    return 1;
                }
            }

            interface IService { }

            class Service : IService
            {
                public Service(IDependency dependency) { }

                public int Value => 1;
            }
            """,
        [CompositionPartFile] = """
            namespace Sample;

            partial class Composition
            {
                private int GetValue()
                {
                    return 1;
                }
            }
            """,
        [ProgramFile] = """
            namespace Sample;

            class Program
            {
                public static void Main()
                {
                    var value = 1;
                }
            }
            """,
        [InterfacesFile] = """
            using Pure.DI;

            namespace Sample;

            [GenerateInterface]
            public partial class Calculator
            {
                public int Add(int a, int b)
                {
                    return a + b;
                }
            }
            """
    };

    [Theory]
    [InlineData(ProgramFile, "var value = 1;", "var value = 2;")]
    [InlineData(ServicesFile, "return 1;", "return 2;\n        // A new line")]
    [InlineData(ServicesFile, "public int Value => 1;", "public int Value => 2;")]
    [InlineData(InterfacesFile, "return a + b;", "return a - b;")]
    public void ShouldSkipGenerationWhenEditCannotChangeCode(string file, string oldText, string newText)
    {
        // Given
        var (driver, compilation) = CreateDriver();

        // When
        compilation = Edit(compilation, file, oldText, newText);
        driver = driver.RunGenerators(compilation);

        // Then
        var result = driver.GetRunResult().Results.Single();
        GetSnapshotReason(result).ShouldBe(IncrementalStepRunReason.Unchanged);
        GetInterfaceReasons(result).ShouldAllBe(reason => reason == IncrementalStepRunReason.Unchanged);
        GetCode(driver.GetRunResult()).ShouldBe(GenerateFromScratch(compilation));
    }

    [Theory]
    [InlineData(SetupFile, "\"Service\"", "\"MyService\"")]
    [InlineData(SetupFile, "namespace Sample;", "namespace Sample;\n")]
    [InlineData(ServicesFile, "public Service(IDependency dependency) { }", "public Service(IDependency dependency, System.Func<IDependency> factory) { }")]
    [InlineData(ServicesFile, "class Dependency : IDependency", "class Dependency : IDependency, System.IDisposable")]
    [InlineData(CompositionPartFile, "return 1;", "return 2;")]
    public void ShouldGenerateWhenEditCanChangeCode(string file, string oldText, string newText)
    {
        // Given
        var (driver, compilation) = CreateDriver();

        // When
        compilation = Edit(compilation, file, oldText, newText);
        driver = driver.RunGenerators(compilation);

        // Then
        GetSnapshotReason(driver.GetRunResult().Results.Single()).ShouldBe(IncrementalStepRunReason.Modified);
        GetCode(driver.GetRunResult()).ShouldBe(GenerateFromScratch(compilation));
    }

    [Fact]
    public void ShouldGenerateInterfaceWhenItsClassChanges()
    {
        // Given
        var (driver, compilation) = CreateDriver();

        // When
        compilation = Edit(compilation, InterfacesFile, "public int Add(int a, int b)", "public int Add(int a, int b, int c)");
        driver = driver.RunGenerators(compilation);

        // Then
        GetInterfaceReasons(driver.GetRunResult().Results.Single()).ShouldContain(IncrementalStepRunReason.Modified);
        var code = GetCode(driver.GetRunResult());
        code.ShouldContain("int Add(int a, int b, int c);");
        code.ShouldBe(GenerateFromScratch(compilation));
    }

    [Fact]
    public void ShouldNotSkipGenerationAfterDiagnosticOutsideSetups()
    {
        // Given
        var (driver, compilation) = CreateDriver();
        compilation = Edit(compilation, ServicesFile, "public Service(IDependency dependency) { }", "public Service(IDependency dependency, IUnknown unknown) { }\n}\n\ninterface IUnknown { }\n\nclass Unused {");
        driver = driver.RunGenerators(compilation);
        var diagnostics = driver.GetRunResult().Diagnostics;
        diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        // The error refers to the constructor parameter, and its position moves with the edit below.
        diagnostics.Select(diagnostic => diagnostic.Location.GetLineSpan().Path).ShouldContain(ServicesFile, string.Join(Environment.NewLine, diagnostics));

        // When
        compilation = Edit(compilation, ServicesFile, "return 1;", "\n\n        return 1;");
        driver = driver.RunGenerators(compilation);

        // Then
        GetSnapshotReason(driver.GetRunResult().Results.Single()).ShouldBe(IncrementalStepRunReason.Modified);

        GetDiagnostics(driver.GetRunResult()).ShouldBe(GetDiagnostics(CreateFromScratch(compilation)));
    }

    private static (GeneratorDriver Driver, Compilation Compilation) CreateDriver()
    {
        var compilation = CSharpCompilation.Create(
            "Sample",
            Sources.Select(source => CSharpSyntaxTree.ParseText(source.Value, ParseOptions, source.Key)),
            TestExtensions.References.Value,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var driver = CreateGeneratorDriver().RunGenerators(compilation);
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        GetCode(result).ShouldContain("partial class Composition");
        GetCode(result).ShouldContain("partial interface ICalculator");
        return (driver, compilation);
    }

    private static GeneratorDriver CreateGeneratorDriver() =>
        CSharpGeneratorDriver.Create(
            [new SourceGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

    // Edits a file the way an IDE does: the edited tree replaces the previous one, and other trees are reused.
    private static Compilation Edit(Compilation compilation, string file, string oldText, string newText)
    {
        var tree = compilation.SyntaxTrees.Single(i => i.FilePath == file);
        var text = tree.GetText().ToString();
        text.ShouldContain(oldText);
        var position = text.IndexOf(oldText, StringComparison.Ordinal);
        var newTree = tree.WithChangedText(tree.GetText().WithChanges(new TextChange(new TextSpan(position, oldText.Length), newText)));
        return compilation.ReplaceSyntaxTree(tree, newTree);
    }

    private static GeneratorDriverRunResult CreateFromScratch(Compilation compilation) =>
        CreateGeneratorDriver().RunGenerators(compilation).GetRunResult();

    private static string GenerateFromScratch(Compilation compilation) =>
        GetCode(CreateFromScratch(compilation));

    private static IncrementalStepRunReason GetSnapshotReason(GeneratorRunResult result) =>
        result.TrackedSteps[SourceGenerator.CompilationSnapshotStep].Single().Outputs.Single().Reason;

    private static IEnumerable<IncrementalStepRunReason> GetInterfaceReasons(GeneratorRunResult result) =>
        result.TrackedSteps[SourceGenerator.InterfacesStep].SelectMany(step => step.Outputs).Select(output => output.Reason);

    private static string GetCode(GeneratorDriverRunResult result) =>
        string.Join(
            Environment.NewLine,
            result.Results.Single().GeneratedSources
                .OrderBy(source => source.HintName, StringComparer.Ordinal)
                .Select(source => $"// {source.HintName}{Environment.NewLine}{source.SourceText}"));

    private static string GetDiagnostics(GeneratorDriverRunResult result) =>
        string.Join(
            Environment.NewLine,
            result.Diagnostics
                .Select(diagnostic => $"{diagnostic.Id} {diagnostic.Location.GetLineSpan()} {diagnostic.GetMessage()}")
                .OrderBy(i => i, StringComparer.Ordinal));
}
