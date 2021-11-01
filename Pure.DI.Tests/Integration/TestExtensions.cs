// ReSharper disable StringLiteralTypo
namespace Pure.DI.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using Core;
    using IoC;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public static class TestExtensions
    {
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
                    MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IServiceProvider).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(MvcServiceCollectionExtensions).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(HttpClient).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IApplicationBuilder).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IMvcBuilder).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(EndpointRoutingApplicationBuilderExtensions).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(WebHostBuilder).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IWebHostEnvironment).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(TestServer).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(DI).Assembly.Location));

        public static IReadOnlyList<string> Run(this string setupCode, out string generatedCode, RunOptions? options = default)
        {
            var curOptions = options ?? new RunOptions();
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(curOptions.LanguageVersion);

            var hostCode = @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Hosting;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.AspNetCore.TestHost;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using System;
            using System.Threading.Tasks;
            using Pure.DI;

            namespace Sample { public class Program { public static void Main() {" + curOptions.Statements + @"} } }";
            var additionalCode = curOptions.AdditionalCode.Select(code => CSharpSyntaxTree.ParseText(code, parseOptions)).ToArray();

            var compilation = CreateCompilation()
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(setupCode, parseOptions))
                .AddSyntaxTrees(additionalCode);

            var stdErr = new StdErr();

            var container = Container
                .Create()
                .Using<Configuration>()
                .Create()
                .Bind<IStdOut>().Bind<IStdErr>().As(Lifetime.Singleton).To(ctx => stdErr)
                .Container;

            var generatedSources = new List<Source>();
            try
            {
                var metadataContext = container.Resolve<IMetadataBuilder>().Build(compilation, CancellationToken.None);
                generatedSources.AddRange(metadataContext.Components);
                generatedSources.AddRange(container.Resolve<ISourceBuilder>().Build(metadataContext));
            }
            catch (BuildException buildException)
            {
                generatedCode = string.Empty;
                return new List<string> { $"{buildException.Id}: {buildException.Message}" };
            }
            catch (HandledException)
            {
                generatedCode = string.Empty;
                return stdErr.Errors;
            }

            generatedCode = string.Join(Environment.NewLine, generatedSources.Select((src, index) => $"Generated {index + 1}" + Environment.NewLine + Environment.NewLine + src.Code));
            
            compilation = compilation
                .WithOptions(
                    new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                        .WithNullableContextOptions(options?.NullableContextOptions ?? NullableContextOptions.Disable))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(hostCode, parseOptions))
                .AddSyntaxTrees(generatedSources.Select(i => CSharpSyntaxTree.ParseText(i.Code.ToString(), parseOptions)).ToArray())
                .Check();

            var tempFileName = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString()[..4]);
            var assemblyPath = Path.ChangeExtension(tempFileName, "exe");
            var configPath = Path.ChangeExtension(tempFileName, "runtimeconfig.json");
            var runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Split(" ")[1];
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
                var output = new List<string>();
                File.WriteAllText(configPath, config);
                var result = compilation.Emit(assemblyPath);
                Assert.True(result.Success);

                void OnOutputDataReceived(object sender, DataReceivedEventArgs args)
                {
                    if (args.Data != null)
                    {
                        output.Add(args.Data);
                    }
                }

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
                    process.OutputDataReceived += OnOutputDataReceived;
                    process.ErrorDataReceived += OnOutputDataReceived;

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }
                finally
                {
                    process.OutputDataReceived -= OnOutputDataReceived;
                    process.ErrorDataReceived -= OnOutputDataReceived;
                }

                output.AddRange(stdErr.Info);
                output.AddRange(stdErr.Errors);
                return output;
            }
            finally
            {
                if (File.Exists(assemblyPath))
                {
                    File.Delete(assemblyPath);
                }

                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }
            }
        }

        private static CSharpCompilation Check(this CSharpCompilation compilation)
        {
            var errors = (
                from diagnostic in compilation.GetDiagnostics()
                where diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning
                select GetErrorMessage(diagnostic))
                .ToList();

            Assert.False(errors.Any(), string.Join(Environment.NewLine + Environment.NewLine, errors));
            return compilation;
        }

        private static string GetErrorMessage(Diagnostic diagnostic)
        {
            var description = diagnostic.GetMessage();
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
                       source.Split(Environment.NewLine)
                           .Select(
                               (line, number) => $"/*{number + 1:0000}*/ {line}")
                   );

        }

        private static string GetSystemAssemblyPathByName(string assemblyName) =>
            Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty, assemblyName);

        private class StdErr: IStdOut, IStdErr
        {
            private readonly List<string> _errors = new();
            private readonly List<string> _lines = new();

            public IEnumerable<string> Info => _lines;

            public IReadOnlyList<string> Errors => _errors;

            public void WriteLine(string line) => _lines.Add(line);

            public void WriteErrorLine(string error) => _errors.Add(error);
        }
    }
}