namespace Pure.DI.Tests.Integration;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


public class RunOptions
{
    public string Statements = "System.Console.WriteLine(Composer.Resolve<CompositionRoot>().Value);";

    public readonly List<string> AdditionalCode = new();

    public LanguageVersion LanguageVersion = LanguageVersion.Latest;

    public NullableContextOptions NullableContextOptions = NullableContextOptions.Disable;
    
    public bool CheckCompilationErrors = true;
}