namespace Pure.DI.Core;

internal interface IContextInitializer
{
    void Initialize(IContextOptions options, IContextProducer producer, IContextDiagnostic diagnostic);
}