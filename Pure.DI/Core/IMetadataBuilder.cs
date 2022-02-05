namespace Pure.DI.Core;

internal interface IMetadataBuilder
{
    MetadataContext Build(IExecutionContext executionContext);
}