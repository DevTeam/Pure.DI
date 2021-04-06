namespace Pure.DI.Core
{
    using System;

    [AttributeUsage(AttributeTargets.Parameter)]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class TagAttribute: Attribute
    {
        public readonly Tags Tag;

        public TagAttribute(Tags tag) =>
            Tag = tag;
    }
}
