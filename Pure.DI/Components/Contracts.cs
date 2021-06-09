// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable StructCanBeMadeReadOnly
// ReSharper disable MemberCanBePrivate.Global
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

        public OrderAttribute(int order)
        {
            Order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    internal class TagAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly object Tag;

        public TagAttribute(object tag)
        {
            Tag = tag;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    internal class TypeAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly Type Type;

        public TypeAttribute(Type type)
        {
            Type = type;
        }
    }
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class IncludeAttribute : Attribute
    {
        public readonly string TypeNameRegularExpression;

        public IncludeAttribute(string typeNameRegularExpression)
        {
            TypeNameRegularExpression = typeNameRegularExpression;
        }
    }
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class ExcludeAttribute : Attribute
    {
        public readonly string TypeNameRegularExpression;

        public ExcludeAttribute(string typeNameRegularExpression)
        {
            TypeNameRegularExpression = typeNameRegularExpression;
        }
    }
    
    internal static class DI
    {
        internal static IConfiguration Setup(string targetTypeName = "")
        {
            return Configuration.Shared;
        }

        private class Configuration : IConfiguration
        {
            public static readonly IConfiguration Shared = new Configuration();

            public IBinding Bind<T>()
            {
                return new Binding(this);
            }

            public IConfiguration DependsOn(string configurationName)
            {
                return this;
            }

            public IConfiguration TypeAttribute<T>(int typeArgumentPosition = 0) where T : Attribute
            {
                return this;
            }

            public IConfiguration TagAttribute<T>(int tagArgumentPosition = 0) where T : Attribute
            {
                return this;
            }

            public IConfiguration OrderAttribute<T>(int orderArgumentPosition = 0) where T : Attribute
            {
                return this;
            }
        }

        private class Binding : IBinding
        {
            private readonly IConfiguration _configuration;

            public Binding(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public IBinding Bind<T>()
            {
                return this;
            }

            public IBinding As(Lifetime lifetime)
            {
                return this;
            }

            public IBinding Tag(object tag)
            {
                return this;
            }

            public IBinding AnyTag()
            {
                return this;
            }

            public IConfiguration To<T>()
            {
                return _configuration;
            }

            public IConfiguration To<T>(Func<IContext, T> factory)
            {
                return _configuration;
            }
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

        IBinding AnyTag();

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

    internal interface IFactory<T>
    {
        T Create(Func<T> factory);
    }

    internal interface IFactory
    {
        T Create<T>(Func<T> factory, object tag);
    }

    internal struct RegisterDisposableEvent
    {
        public readonly IDisposable Disposable;
        public readonly Lifetime Lifetime;

        public RegisterDisposableEvent(IDisposable disposable, Lifetime lifetime)
        {
            Disposable = disposable;
            Lifetime = lifetime;
        }
    }

    internal delegate void RegisterDisposable(RegisterDisposableEvent registerDisposableEvent);
}