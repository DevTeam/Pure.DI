namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SyntaxRegistry : CSharpSyntaxWalker, ISyntaxRegistry
    {
        private readonly IEnumerable<SyntaxTree> _syntaxTrees;
        private  MethodDeclarationSyntax? _method;
        private string? _methodName;
        private string? _className;

        public SyntaxRegistry(IBuildContext buildContext)
        {
            _syntaxTrees = buildContext.Compilation.SyntaxTrees;
        }

        public MethodDeclarationSyntax FindMethod(string className, string methodName)
        {
            _className = className;
            _methodName = methodName;
            _method = null;
            foreach (var syntaxTree in _syntaxTrees)
            {
                Visit(syntaxTree.GetRoot());
                if (_method != null)
                {
                    return _method;
                }
            }

            throw new InvalidOperationException($"Cannot find method {methodName} in class {className}.");
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (
                node.Identifier.Text == _methodName 
                && node.Ancestors().OfType<ClassDeclarationSyntax>().Any(i => i.Identifier.Text == _className))
            {
                _method = node;
            }
            else
            {
                base.VisitMethodDeclaration(node);
            }
        }
    }
}
