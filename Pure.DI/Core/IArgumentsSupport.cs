namespace Pure.DI.Core;

internal interface IArgumentsSupport
{
    IEnumerable<ParameterSyntax> GetParameters();

    IEnumerable<ArgumentSyntax> GetArguments();

    SimpleLambdaExpressionSyntax CreateArgumentFactory(ArgumentMetadata arg);
}