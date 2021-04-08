namespace Pure.DI
{
    using System;

    [AttributeUsage(AttributeTargets.Parameter)]
    public class TypeAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly Type Type;

        public TypeAttribute(Type type) => Type = type;
    }
}
