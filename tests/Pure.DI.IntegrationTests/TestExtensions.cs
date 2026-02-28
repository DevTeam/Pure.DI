// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ImplicitCapture
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable HeapView.BoxingAllocation

// ReSharper disable UnusedMethodReturnValue.Local
namespace Pure.DI.IntegrationTests;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Runtime.Loader;
using Core;
using Core.Models;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Generator=Generator;

public static class TestExtensions
{
    private static readonly AsyncLocal<StringWriter?> OutputWriter = new();

    static TestExtensions()
    {
        var initialOut = Console.Out;
        Console.SetOut(new ThreadSafeConsoleWriter(initialOut));
        var initialError = Console.Error;
        Console.SetError(new ThreadSafeConsoleWriter(initialError));
    }

    private class ThreadSafeConsoleWriter(TextWriter initialOut) : TextWriter
    {
        public override System.Text.Encoding Encoding => initialOut.Encoding;

        public override void Write(char value)
        {
            OutputWriter.Value?.Write(value);
            initialOut.Write(value);
        }

        public override void Write(char[]? buffer, int index, int count)
        {
            if (buffer == null)
            {
                return;
            }

            OutputWriter.Value?.Write(buffer, index, count);
            initialOut.Write(buffer, index, count);
        }

        public override void Write(string? value)
        {
            OutputWriter.Value?.Write(value);
            initialOut.Write(value);
        }

        public override void Write(ReadOnlySpan<char> buffer)
        {
            OutputWriter.Value?.Write(buffer);
            initialOut.Write(buffer);
        }

        public override void WriteLine()
        {
            OutputWriter.Value?.WriteLine();
            initialOut.WriteLine();
        }

        public override void WriteLine(string? value)
        {
            OutputWriter.Value?.WriteLine(value);
            initialOut.WriteLine(value);
        }

        public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            OutputWriter.Value?.WriteLine(buffer);
            initialOut.WriteLine(buffer);
        }
    }

    [SuppressMessage("Performance", "CA1806:Не игнорируйте результаты метода")]
    internal static Task<Result> RunAsync(this string setupCode, Options? options = null)
    {
        return new[] { setupCode }.RunAsync(options);
    }

    [SuppressMessage("Performance", "CA1806:Не игнорируйте результаты метода")]
    internal static Task<Result> RunAsync(this IEnumerable<string> sourceTexts, Options? options = null)
    {
        var stdOut = new List<string>();
        var sourceList = sourceTexts.ToList();
        if (sourceList.Count == 0)
        {
            throw new ArgumentException("At least one source is required.", nameof(sourceTexts));
        }

        var runOptions = options ?? new Options();
        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(runOptions.LanguageVersion);

        parseOptions = runOptions.PreprocessorSymbols.IsDefaultOrEmpty
            ? parseOptions.WithPreprocessorSymbols("NET", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER")
            : parseOptions.WithPreprocessorSymbols(runOptions.PreprocessorSymbols);

        var generator = new Generator();
        var generatedApiSources = generator.Api.ToArray();
        var compilation = CreateCompilation()
            .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithNullableContextOptions(runOptions.NullableContextOptions))
            .AddSyntaxTrees(generatedApiSources.Select(api => CSharpSyntaxTree.ParseText(api.SourceText, parseOptions)))
            .AddSyntaxTrees(sourceList.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions)));
        // .Check(stdOut, options);

        var globalOptions = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            { GlobalProperties.SeverityProperty, nameof(DiagnosticSeverity.Info) },
            { GlobalProperties.LogFileProperty, ".logs\\IntegrationTests.log" },
            { GlobalProperties.CultureProperty, options?.Culture ?? CultureInfo.CurrentUICulture.Name }
        });

        var generatedSources = new List<Source>();
        var contextOptions = new Mock<IGeneratorOptions>();
        contextOptions.SetupGet(i => i.GlobalOptions).Returns(() => globalOptions);
        var sources = new Mock<ISources>();
        var contextDiagnostic = new Mock<IGeneratorDiagnostic>();
        sources.Setup(i => i.AddSource(It.IsAny<string>(), It.IsAny<SourceText>()))
            .Callback<string, SourceText>((hintName, sourceText) => { generatedSources.Add(new Source(hintName, sourceText)); });

        var compilationCopy = compilation;
        var updates = compilationCopy.SyntaxTrees.Select(i => CreateUpdate(i, compilationCopy));

        var dependencyGraphObserver = new Observer<DependencyGraph>();
        using var dependencyGraphObserverToken = generator.Observers.Register(dependencyGraphObserver);

        var logEntryObserver = new Observer<LogEntry>();
        using var logEntryObserverToken = generator.Observers.Register(logEntryObserver);

        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        generator.Generate(contextOptions.Object, sources.Object, contextDiagnostic.Object, [..updates], CancellationToken.None);

        var logs = logEntryObserver.Values;
        var errors = logs.Where(i => i.Severity == DiagnosticSeverity.Error).ToImmutableArray();
        var warnings = logs.Where(i => i.Severity == DiagnosticSeverity.Warning).ToImmutableArray();
        if (errors.Any())
        {
            return Task.FromResult(new Result(
                false,
                [..stdOut],
                logs,
                errors,
                warnings,
                dependencyGraphObserver.Values,
                string.Empty));
        }

        compilation = compilation
            .AddSyntaxTrees(generatedSources.Select(i => CSharpSyntaxTree.ParseText(i.SourceText, parseOptions)).ToArray());

        generatedSources.AddRange(generatedApiSources);
        var generatedCode = string.Join(Environment.NewLine, generatedSources.Select(i => i.SourceText));

        compilation.Check(stdOut, options);

        using var assemblyStream = new MemoryStream();
        var result = compilation.Emit(assemblyStream);
        if (options?.CheckCompilationErrors ?? true)
        {
            Assert.True(result.Success);
        }

        if (!result.Success)
        {
            return Task.FromResult(new Result(
                false,
                [..stdOut],
                logs,
                errors,
                warnings,
                dependencyGraphObserver.Values,
                generatedCode));
        }

        assemblyStream.Seek(0, SeekOrigin.Begin);
        var assemblyLoadContext = new AssemblyLoadContext("RunContext", true);
        var isExecutionSuccess = true;
        try
        {
            var assembly = assemblyLoadContext.LoadFromStream(assemblyStream);
            var entryPoint = assembly.EntryPoint;
            if (entryPoint == null)
            {
                return Task.FromResult(new Result(
                    false,
                    ["No entry point found."],
                    logs,
                    errors,
                    warnings,
                    dependencyGraphObserver.Values,
                    generatedCode));
            }

            try
            {
                using var stringWriter = new StringWriter();
                OutputWriter.Value = stringWriter;
                try
                {
                    object?[]? args = entryPoint.GetParameters().Length > 0 ? [Array.Empty<string>()] : null;
                    entryPoint.Invoke(null, args);
                }
                catch (System.Reflection.TargetInvocationException ex)
                {
                    isExecutionSuccess = false;
                    stdOut.Add(ex.InnerException?.ToString() ?? ex.ToString());
                }
                catch (Exception ex)
                {
                    isExecutionSuccess = false;
                    stdOut.Add(ex.ToString());
                }
                finally
                {
                    stringWriter.Flush();
                    var output = stringWriter.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                    stdOut.AddRange(output);
                }
            }
            finally
            {
                OutputWriter.Value = null;
            }
        }
        finally
        {
            assemblyLoadContext.Unload();
        }

        return Task.FromResult(new Result(
            isExecutionSuccess && !errors.Any() && !warnings.Any(),
            [..stdOut],
            logs,
            errors,
            warnings,
            dependencyGraphObserver.Values,
            generatedCode));
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
                MetadataReference.CreateFromFile(typeof(ConcurrentBag<object>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Regex).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(INotifyPropertyChanged).Assembly.Location));

    private static CSharpCompilation Check(this CSharpCompilation compilation, List<string> output, Options? options)
    {
        var diagnostics = (
            from diagnostic in compilation.GetDiagnostics()
            where diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning
            select diagnostic)
            .ToList();

        if (diagnostics.Count == 0)
        {
            return compilation;
        }

        output.Add($"Language version is {compilation.LanguageVersion}");
        output.Add(Environment.NewLine);
        foreach (var diagnosticsBySourceTree in diagnostics.GroupBy(i => i.Location.SourceTree!))
        {
            output.AddRange(diagnosticsBySourceTree.Select(diagnostic => diagnostic.ToString()));
            output.Add(Environment.NewLine);

            var sourceCode = diagnosticsBySourceTree.Key.ToString();
            output.AddRange(AddLineNumbers(sourceCode));
            output.Add(Environment.NewLine);
        }

        if (!(options?.CheckCompilationErrors ?? true))
        {
            return compilation;
        }

        Assert.True(diagnostics.Count == 0, string.Join(Environment.NewLine, output));
        return compilation;
    }

    public static string GetSource(this Location? location)
    {
        if (location is null || !location.IsInSource)
        {
            return "unknown";
        }

        var source = location.SourceTree.ToString();
        return source.Substring(location.SourceSpan.Start, location.SourceSpan.Length);
    }

    private static IEnumerable<string> AddLineNumbers(string source) =>
        source.Split(Environment.NewLine).Select((line, number) => $"/*{number + 1:0000}*/ {line}");

    private static string GetSystemAssemblyPathByName(string assemblyName) =>
        Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty, assemblyName);

    private class TestAnalyzerConfigOptions(IDictionary<string, string> options)
        : AnalyzerConfigOptions
    {
#pragma warning disable CS8765
        public override bool TryGetValue(string key, [UnscopedRef] out string? value)
#pragma warning restore CS8765
            => options.TryGetValue(key, out value);
    }

    private class Observer<T> : IObserver<T>
    {
        private readonly List<T> _values = [];

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
