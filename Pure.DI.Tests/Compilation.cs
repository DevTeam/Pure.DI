namespace Pure.DI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public static class Compilation
    {
        public static (CSharpCompilation compilation, SyntaxTree tree, CompilationUnitSyntax root, SemanticModel semanticModel) Compile(this string code)
        {
            var compilation = CSharpCompilation
                .Create("Sample")
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(GetSystemAssemblyPathByName("netstandard.dll")),
                    MetadataReference.CreateFromFile(GetSystemAssemblyPathByName("System.Runtime.dll")),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(DI).Assembly.Location))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

            var tree = compilation.SyntaxTrees.Single();
            var root = tree.GetCompilationUnitRoot();
            var semanticModel = compilation.GetSemanticModel(tree);
            var diagnostics = semanticModel.GetDiagnostics();
            if (diagnostics.Any())
            {
                throw new ArgumentException(string.Join(Environment.NewLine, diagnostics.Select(i => i.GetMessage())), nameof(code));
            }

            return (compilation, tree, root, semanticModel);
        }

        private static string GetSystemAssemblyPathByName(string assemblyName) =>
            Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty, assemblyName);
    }
}