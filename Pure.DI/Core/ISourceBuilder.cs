namespace Pure.DI.Core
{
    using System.Collections.Generic;

    internal interface ISourceBuilder
    {
        IEnumerable<Source>Build(MetadataContext context);
    }
}