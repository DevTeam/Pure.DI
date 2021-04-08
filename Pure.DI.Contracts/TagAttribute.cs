namespace Pure.DI
{
    using System;

    [AttributeUsage(AttributeTargets.Parameter)]
    public class TagAttribute: Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly object Tag;

        public TagAttribute(object tag) => Tag = tag;
    }
}
