namespace Pure.DI.Core;

internal interface IArgumentsSupport
{
    IEnumerable<ArgumentMetadata> GetArgumentsMetadata();

    IEnumerable<ParameterSyntax> GetParameters();

    IEnumerable<ArgumentSyntax> GetArguments();

    SimpleLambdaExpressionSyntax CreateArgumentFactory(ArgumentMetadata arg);
}