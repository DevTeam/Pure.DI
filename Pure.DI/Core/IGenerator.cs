namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    internal interface IGenerator
    {
        void Generate(GeneratorExecutionContext context);
    }
}