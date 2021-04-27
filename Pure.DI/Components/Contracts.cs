// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedField.Global
#pragma warning disable 0436
namespace Pure.DI
{
    using System;

    internal enum Lifetime
    {
        /// <summary>
        /// Creates a new object of the requested type every time.
        /// </summary>
        Transient,

        /// <summary>
        /// Creates a singleton object first time you and then returns the same object.
        /// </summary>
        Singleton,

        /// <summary>
        /// Creates a singleton object per thread. It returns different objects on different threads.
        /// </summary>
        PerThread,

        /// <summary>
        /// Similar to the Transient, but it reuses the same object in the recursive object graph.
        /// </summary>
        PerResolve,

        /// <summary>
        /// This lifetime allows to apply a custom lifetime to a binding. Just realize the interface <c>ILifetime&lt;T&gt;</c> and bind, for example: .Bind&lt;ILifetime&lt;IMyInterface&gt;&gt;().To&lt;MyLifetime&gt;()
        /// </summary>
        Binding,

        /// <summary>
        /// This lifetime is applicable for integration with Microsoft Dependency Injection.
        /// Specifies that a single instance of the service will be created.
        /// </summary>
        ContainerSingleton,

        /// <summary>
        /// This lifetime is applicable for integration with Microsoft Dependency Injection.
        /// Specifies that a new instance of the service will be created for each scope.
        /// </summary>
        Scoped
    }

    /// <summary>
    /// Represents the generic type arguments marker.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    internal sealed class GenericTypeArgumentAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
    internal class OrderAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly int Order;

        public OrderAttribute(int order) => Order = order;
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    internal class TagAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly object Tag;

        public TagAttribute(object tag) => Tag = tag;
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    internal class TypeAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly Type Type;

        public TypeAttribute(Type type) => Type = type;
    }

    internal static class DI
    {
        internal static IConfiguration Setup(string targetTypeName = "") => Configuration.Shared;

        private class Configuration : IConfiguration
        {
            public static readonly IConfiguration Shared = new Configuration();

            public IBinding Bind<T>() => new Binding(this);

            public IConfiguration DependsOn(string configurationName) => this;

            public IConfiguration TypeAttribute<T>(int typeArgumentPosition = 0) where T : Attribute => this;

            public IConfiguration TagAttribute<T>(int tagArgumentPosition = 0) where T : Attribute => this;

            public IConfiguration OrderAttribute<T>(int orderArgumentPosition = 0) where T : Attribute => this;
        }

        private class Binding : IBinding
        {
            private readonly IConfiguration _configuration;

            public Binding(IConfiguration configuration) => _configuration = configuration;

            public IBinding Bind<T>() => this;

            public IBinding As(Lifetime lifetime) => this;

            public IBinding Tag(object tag) => this;

            public IConfiguration To<T>() => _configuration;

            public IConfiguration To<T>(Func<IContext, T> factory) => _configuration;
        }
    }

    internal interface IConfiguration
    {
        IBinding Bind<T>();

        IConfiguration DependsOn(string configurationName);

        IConfiguration TypeAttribute<T>(int typeArgumentPosition = 0) where T : Attribute;

        IConfiguration TagAttribute<T>(int tagArgumentPosition = 0) where T : Attribute;

        IConfiguration OrderAttribute<T>(int orderArgumentPosition = 0) where T : Attribute;
    }

    internal interface IBinding
    {
        IBinding Bind<T>();

        IBinding As(Lifetime lifetime);

        IBinding Tag(object tag);

        IConfiguration To<T>();

        IConfiguration To<T>(Func<IContext, T> factory);
    }

    internal interface IContext
    {
        T Resolve<T>();

        T Resolve<T>(object tag);
    }

    internal interface IFallback
    {
        object Resolve(Type type, object tag);
    }

    internal interface ILifetime<T>
    {
        T Resolve(Func<T> factory);
    }
}