namespace Pure.DI
{
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.IO;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;
    using Microsoft.CSharp;

    [Generator]
    public class SourceGenerator: ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            /*if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }*/
#endif

            //context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var method = new CodeMemberMethod {Name = "Write", Attributes = MemberAttributes.Public, ReturnType = new CodeTypeReference(typeof(void))};
            method.Statements.Add(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression(typeof(System.Console)),
                        nameof(System.Console.WriteLine)
                        ),
                    new CodePrimitiveExpression("Hello World!")
                    )
                );

            var type = new CodeTypeDeclaration("HelloWorld");
            type.Members.Add(method);

            var ns = new CodeNamespace(context.Compilation.AssemblyName);
            ns.Types.Add(type);

            var codeProvider = new CSharpCodeProvider();
            var sb = new StringBuilder();
            codeProvider.GenerateCodeFromNamespace(ns, new StringWriter(sb), new CodeGeneratorOptions { BlankLinesBetweenMembers = true, IndentString = "    ", BracingStyle = System.Environment.NewLine });

            // inject the created source into the users compilation
            context.AddSource("HelloWorldGenerated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}