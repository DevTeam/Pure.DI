namespace Pure.DI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Core;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Xunit;

    public static class TestExtensions
    {
        public static CSharpCompilation CreateCompilation() =>
            CSharpCompilation
                .Create("Sample")
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(GetSystemAssemblyPathByName("netstandard.dll")),
                    MetadataReference.CreateFromFile(GetSystemAssemblyPathByName("System.Runtime.dll")),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IList<object>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(DI).Assembly.Location));

        private const string Code = @"namespace Sample { public class Program { public static void Main() => System.Console.WriteLine(Composer.Resolve<CompositionRoot>().Value); } }";

        public static IReadOnlyList<string> Run(this string setupCode, out string generatedCode, string resolveName = "Composer")
        {
            var setupCompilation = CreateCompilation()
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(setupCode))
                .Check();

            var generatedSources = new SourceBuilder().Build(setupCompilation).ToList();

            generatedCode = string.Join(Environment.NewLine, generatedSources.Select((src, index) => $"Generated {index + 1}" + Environment.NewLine + Environment.NewLine + src.Code));
            var compilation = CreateCompilation()
                .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(setupCode))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(Code.Replace("Composer", resolveName)))
                .AddSyntaxTrees(generatedSources.Select(i => CSharpSyntaxTree.ParseText(i.Code.ToString())).ToArray())
                .Check();

            var tempFileName = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString().Substring(0, 4));
            var assemblyPath = Path.ChangeExtension(tempFileName, "exe");
            var configPath = Path.ChangeExtension(tempFileName, "runtimeconfig.json");

            var config = @"
{
  ""runtimeOptions"": {
            ""tfm"": ""net5.0"",
            ""framework"": {
                ""name"": ""Microsoft.NETCore.App"",
                ""version"": ""5.0.0""
            }
        }
}";
            try
            {
                File.WriteAllText(configPath, config);
                var result = compilation.Emit(assemblyPath);
                Assert.True(result.Success);

                var output = new List<string>();
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

        public static CSharpCompilation Check(this CSharpCompilation compilation)
        {
            var errors = (
                from diagnostic in compilation.GetDiagnostics()
                where diagnostic.Severity == DiagnosticSeverity.Error
                select GetErrorMessage(diagnostic))
                .ToList();

            Assert.False(errors.Any(), string.Join(Environment.NewLine + Environment.NewLine, errors));
            return compilation;
        }

        private static string GetErrorMessage(Diagnostic diagnostic)
        {
            var description = diagnostic.GetMessage();
            if (diagnostic.Location.IsInSource)
            {
                var source = diagnostic.Location.SourceTree.ToString();
                var span = source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
                return description
                       + Environment.NewLine + Environment.NewLine
                       + span
                       + Environment.NewLine
                       + Environment.NewLine
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

            return description;
        }

        private static string GetSystemAssemblyPathByName(string assemblyName) =>
            Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty, assemblyName);
    }
}