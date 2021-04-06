namespace Pure.DI
{
    using System;

    /// <summary>
    /// Represents the generic type arguments marker.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public sealed class GenericTypeArgumentAttribute : Attribute { }
}
