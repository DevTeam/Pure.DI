// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ImplicitCapture
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI.IntegrationTests;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Core;
using Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Moq;

public static class TestExtensions
{
    internal static async Task<Result> RunAsync(this string setupCode, Options? options = default)
    {
        var stdOut = new List<string>();
        var runOptions = options ?? new Options();
        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(runOptions.LanguageVersion);

        parseOptions = runOptions.PreprocessorSymbols.IsDefaultOrEmpty 
            ? parseOptions.WithPreprocessorSymbols("NET")
            : parseOptions.WithPreprocessorSymbols(runOptions.PreprocessorSymbols);

        var baseComposition = new CompositionBase();
        var generatedApiSources = baseComposition.ApiBuilder.Build(Unit.Shared, CancellationToken.None).ToArray();
        var compilation = CreateCompilation()
            .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithNullableContextOptions(runOptions.NullableContextOptions))
            .AddSyntaxTrees(generatedApiSources.Select(api => CSharpSyntaxTree.ParseText(api.SourceText, parseOptions)))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(setupCode, parseOptions));
            //.Check(stdOut, options);

        var globalOptions = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            { GlobalSettings.Severity, DiagnosticSeverity.Info.ToString() },
            { GlobalSettings.LogFile, ".logs\\IntegrationTests.log" }
        });

        var generatedSources = new List<Source>();
        var contextOptions = new Mock<IContextOptions>();
        contextOptions.SetupGet(i => i.GlobalOptions).Returns(() => globalOptions);
        var contextProducer = new Mock<IContextProducer>();
        var contextDiagnostic = new Mock<IContextDiagnostic>();
        contextProducer.Setup(i => i.AddSource(It.IsAny<string>(), It.IsAny<SourceText>()))
            .Callback<string, SourceText>((hintName, sourceText) => { generatedSources.Add(new Source(hintName, sourceText)); });

        var compilationCopy = compilation;
        var updates = compilationCopy.SyntaxTrees.Select(i => CreateUpdate(i, compilationCopy));
        var composition = new Composition(contextOptions.Object, contextProducer.Object, contextDiagnostic.Object);
        
        var dependencyGraphObserver = new Observer<DependencyGraph>();
        using var dependencyGraphObserverToken = composition.ObserversRegistry.Register(dependencyGraphObserver);

        var logEntryObserver = new Observer<LogEntry>();
        using var logEntryObserverToken = composition.ObserversRegistry.Register(logEntryObserver);
        
        composition.Generator.Build(updates, CancellationToken.None);
        
        var logs = logEntryObserver.Values;
        var errors = logs.Where(i => i.Severity == DiagnosticSeverity.Error).ToImmutableArray();
        var warnings = logs.Where(i => i.Severity == DiagnosticSeverity.Warning).ToImmutableArray();
        if (errors.Any())
        {
            return new Result(
                false,
                stdOut.ToImmutableArray(),
                logs,
                errors,
                warnings,
                dependencyGraphObserver.Values.ToImmutableArray(),
                string.Empty);
        }

        compilation = compilation
            .AddSyntaxTrees(generatedSources.Select(i => CSharpSyntaxTree.ParseText(i.SourceText, parseOptions)).ToArray())
            .Check(stdOut, options);

        generatedSources.AddRange(generatedApiSources);
        var generatedCode = string.Join(Environment.NewLine, generatedSources.Select((src, index) => $"Generated {index + 1}" + Environment.NewLine + Environment.NewLine + src.SourceText));

        var tempFileName = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString()[..4]);
        var assemblyPath = Path.ChangeExtension(tempFileName, "exe");
        var configPath = Path.ChangeExtension(tempFileName, "runtimeconfig.json");
        var runtime = RuntimeInformation.FrameworkDescription.Split(" ")[1];
        var dotnetVersion = $"{Environment.Version.Major}.{Environment.Version.Minor}";
        var config = @"
{
  ""runtimeOptions"": {
            ""tfm"": ""netV.V"",
            ""framework"": {
                ""name"": ""Microsoft.NETCore.App"",
                ""version"": ""RUNTIME""
            }
        }
}".Replace("V.V", dotnetVersion).Replace("RUNTIME", runtime);

        try
        {
            var result = compilation.Emit(assemblyPath);
            if (options?.CheckCompilationErrors ?? true)
            {
                Assert.True(result.Success);
            }

            if (!result.Success)
            {
                return new Result(
                    false,
                    stdOut.ToImmutableArray(),
                    logs,
                    errors,
                    warnings,
                    dependencyGraphObserver.Values.ToImmutableArray(),
                    generatedCode);
            }

            void StdOutReceived(object sender, DataReceivedEventArgs args)
            {
                if (args.Data != null)
                {
                    stdOut.Add(args.Data);
                }
            }
            
            await File.WriteAllTextAsync(configPath, config);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = "dotnet",
                    Arguments = assemblyPath
                }
            };

            try
            {
                process.OutputDataReceived += StdOutReceived;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();
            }
            finally
            {
                process.OutputDataReceived -= StdOutReceived;
            }
            
            return new Result(
                    !errors.Any() && !warnings.Any(),
                stdOut.ToImmutableArray(),
                logs,
                errors,
                warnings,
                dependencyGraphObserver.Values.ToImmutableArray(),
                generatedCode);
        }
        finally
        {
            var attempts = 3;
            while (attempts-- > 0)
            {
                try
                {
                    if (File.Exists(assemblyPath))
                    {
                        File.Delete(assemblyPath);
                    }

                    if (File.Exists(configPath))
                    {
                        File.Delete(configPath);
                    }

                    attempts = 0;
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(300);
                }
            }
        }
    }

    private static SyntaxUpdate CreateUpdate(SyntaxTree syntaxTree, Compilation compilation)
    {
        var rootNode = syntaxTree.GetRoot();
        return new SyntaxUpdate(rootNode, compilation.GetSemanticModel(syntaxTree));
    }

    private static CSharpCompilation CreateCompilation() =>
        CSharpCompilation
            .Create("Sample")
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(GetSystemAssemblyPathByName("netstandard.dll")),
                MetadataReference.CreateFromFile(GetSystemAssemblyPathByName("System.Runtime.dll")),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IList<object>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IImmutableList<object>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(SortedSet<object>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IContextOptions).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(SourceGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Regex).Assembly.Location));
    
    private static CSharpCompilation Check(this CSharpCompilation compilation, List<string> output, Options? options)
    {
        var errors = (
                from diagnostic in compilation.GetDiagnostics()
                where diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning
                select GetErrorMessage(diagnostic))
            .ToList();
        
        output.AddRange(errors);

        if (!(options?.CheckCompilationErrors ?? true))
        {
            return compilation;
        }
        
        var hasError = errors.Any();
        if (!hasError)
        {
            return compilation;
        }

        errors.Insert(0, $"Language version is {compilation.LanguageVersion}, available versions are ... {string.Join(", ", Enum.GetNames<LanguageVersion>().TakeLast(5))}");
        Assert.False(hasError, string.Join(Environment.NewLine + Environment.NewLine, errors));

        return compilation;
    }

    private static string GetErrorMessage(Diagnostic diagnostic)
    {
        var description = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        if (!diagnostic.Location.IsInSource)
        {
            return description;
        }

        var source = diagnostic.Location.SourceTree.ToString();
        var span = source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
        return description
               + Environment.NewLine + Environment.NewLine
               + diagnostic
               + Environment.NewLine + Environment.NewLine
               + span
               + Environment.NewLine + Environment.NewLine
               + "Line " + (diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line + 1)
               + Environment.NewLine
               + Environment.NewLine
               + string.Join(
                   Environment.NewLine,
                   source.Split(Environment.NewLine).Select((line, number) => $"/*{number + 1:0000}*/ {line}")
               );
    }

    private static string GetSystemAssemblyPathByName(string assemblyName) =>
        Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty, assemblyName);
    
    private class TestAnalyzerConfigOptions: AnalyzerConfigOptions
    {
        private readonly IDictionary<string, string> _options;

        public TestAnalyzerConfigOptions(IDictionary<string, string> options) => _options = options;

#pragma warning disable CS8765
        public override bool TryGetValue(string key, [UnscopedRef] out string? value)
#pragma warning restore CS8765
            => _options.TryGetValue(key, out value);
    }
    
    private class Observer<T>: IObserver<T>
    {
        private readonly List<T> _values = new();

        public IReadOnlyList<T> Values => _values;

        public void OnNext(T value) => _values.Add(value);

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }
}