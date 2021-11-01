namespace Pure.DI.Core
{
    using System.Threading;
    using Microsoft.CodeAnalysis;

    internal interface IMetadataBuilder
    {
        MetadataContext Build(Compilation compilation, CancellationToken cancellationToken);
    }
}