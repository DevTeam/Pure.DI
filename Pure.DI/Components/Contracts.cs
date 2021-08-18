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

    /// <summary>
    /// The abstraction of a binding lifetime.
    /// </summary>
    internal enum Lifetime
    {
        /// <summary>
        /// Creates a new object of the requested type every time. To manage disposable instances subscribe on "OnDisposable" event: <example>Composer.OnDisposable += e => disposables.Add(e.Disposable);</example> and dispose instances at appropriate time.
        /// </summary>
        Transient,

        /// <summary>
        /// Creates a <seealso href="https://github.com/DevTeam/Pure.DI#singleton-lifetime-">singleton</seealso> object first time you and then returns the same object. To dispose <see cref="IDisposable"/> instances use this approach: <c>Composer.FinalDispose();</c>.
        /// </summary>
        Singleton,

        /// <summary>
        /// <seealso href="https://github.com/DevTeam/Pure.DI#per-resolve-lifetime-">The per resolve lifetime</seealso> is similar to the Transient, but it reuses the same object in the recursive object graph. To manage disposable instances subscribe on "OnDisposable" event: <example>Composer.OnDisposable += e => disposables.Add(e.Disposable);</example> and dispose instances and dispose instances at appropriate time.
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
    /// Represents the generic type arguments marker. It allows creating custom generic type arguments marker like <see cref="TTS"/>, <see cref="TTDictionary{TKey,TValue}"/> and etc. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    internal sealed class GenericTypeArgumentAttribute : Attribute { }
    
    /// <summary>
    /// Represents a <seealso href="https://github.com/DevTeam/Pure.DI#aspect-oriented-di-">custom attribute</seealso> overriding an injection order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
    internal class OrderAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// The injection order.
        /// </summary>
        public readonly int Order;

        /// <summary>
        /// Creates an attribute instance.
        /// </summary>
        /// <param name="order">The injection order.</param>
        public OrderAttribute(int order)
        {
            Order = order;
        }
    }

    /// <summary>
    /// Represents a <seealso href="https://github.com/DevTeam/Pure.DI#aspect-oriented-di-">tag attribute</seealso> overriding an injection tag.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
    internal class TagAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// The injection tag.
        /// </summary>
        public readonly object Tag;

        /// <summary>
        /// Creates an attribute instance.
        /// </summary>
        /// <param name="tag">The injection tag. See also <see cref="IBinding.Tag"/></param>.
        public TagAttribute(object tag)
        {
            Tag = tag;
        }
    }

    /// <summary>
    /// Represents a <seealso href="https://github.com/DevTeam/Pure.DI#aspect-oriented-di-">custom attribute</seealso> overriding an injection type. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
    internal class TypeAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// The injection type.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Creates an attribute instance.
        /// </summary>
        /// <param name="type">The injection type. See also <see cref="IConfiguration.Bind{T}"/> and <see cref="IBinding.Bind{T}"/>.</param>
        public TypeAttribute(Type type)
        {
            Type = type;
        }
    }
    
    /// <summary>
    /// Represent <seealso href="https://github.com/DevTeam/Pure.DI#intercept-many-">the type including regular expression filter</seealso> for a <c>IFactory</c> which are processing. Is used together with <see cref="IFactory"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class IncludeAttribute : Attribute
    {
        /// <summary>
        /// The regular expression for full type names.
        /// </summary>
        public readonly string TypeNameRegularExpression;

        /// <summary>
        /// Creates an attribute instance.
        /// </summary>
        /// <param name="typeNameRegularExpression">The regular expression for full type names. For example <example>MyClass.+</example>.</param>
        public IncludeAttribute(string typeNameRegularExpression)
        {
            TypeNameRegularExpression = typeNameRegularExpression;
        }
    }
    
    /// <summary>
    /// Represent <seealso href="https://github.com/DevTeam/Pure.DI#intercept-many-">the excluding regular expression filter</seealso> for a <c>IFactory</c> which are not processing. Is used together with <see cref="IFactory"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class ExcludeAttribute : Attribute
    {
        /// <summary>
        /// The regular expression for full type names.
        /// </summary>
        public readonly string TypeNameRegularExpression;

        /// <summary>
        /// Creates an attribute instance.
        /// </summary>
        /// <param name="typeNameRegularExpression">The regular expression for full type names. For example <example>MyClass.+</example>.</param>
        public ExcludeAttribute(string typeNameRegularExpression)
        {
            TypeNameRegularExpression = typeNameRegularExpression;
        }
    }
    
    /// <summary>
    /// Represent the static instance to configure a DI composer.
    /// </summary>
    internal static class DI
    {
        /// <summary>
        /// Starts DI configuration chain.
        /// </summary>
        /// <param name="composerTypeName">The optional argument to specify a custom DI composer type name to generate. By default, it is a name of an owner class if the owner class is <c>static partial class</c> otherwise, it is a name of an owner plus the "DI" postfix.</param>
        /// <returns>DI configuration.</returns>
        internal static IConfiguration Setup(string composerTypeName = "")
        {
            return Configuration.Shared;
        }

        private class Configuration : IConfiguration
        {
            public static readonly IConfiguration Shared = new Configuration();

            /// <inheritdoc />
            public IBinding Bind<T>()
            {
                return new Binding(this);
            }

            /// <inheritdoc />
            public IConfiguration DependsOn(string baseConfigurationName)
            {
                return this;
            }

            /// <inheritdoc />
            public IConfiguration TypeAttribute<T>(int typeArgumentPosition = 0) where T : Attribute
            {
                return this;
            }

            /// <inheritdoc />
            public IConfiguration TagAttribute<T>(int tagArgumentPosition = 0) where T : Attribute
            {
                return this;
            }

            /// <inheritdoc />
            public IConfiguration OrderAttribute<T>(int orderArgumentPosition = 0) where T : Attribute
            {
                return this;
            }

            /// <inheritdoc />
            public IConfiguration Default(Lifetime lifetime)
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

            /// <inheritdoc />
            public IBinding Bind<T>()
            {
                return this;
            }

            /// <inheritdoc />
            public IBinding As(Lifetime lifetime)
            {
                return this;
            }

            /// <inheritdoc />
            public IBinding Tag(object tag)
            {
                return this;
            }

            /// <inheritdoc />
            public IBinding AnyTag()
            {
                return this;
            }

            /// <inheritdoc />
            public IConfiguration To<T>()
            {
                return _configuration;
            }

            /// <inheritdoc />
            public IConfiguration To<T>(Func<IContext, T> factory)
            {
                return _configuration;
            }
        }
    }

    /// <summary>
    /// The abstraction to configure DI.
    /// </summary>
    internal interface IConfiguration
    {
        /// <summary>
        /// Starts a binding configuration chain.
        /// </summary>
        /// <typeparam name="T">The type of dependency to bind. Also supports generic type markers like <see cref="TT"/>, <see cref="TTList{T}"/> and others.</typeparam>
        /// <returns>Binding configuration.</returns>
        IBinding Bind<T>();

        /// <summary>
        /// Use a DI configuration as a base.
        /// </summary>
        /// <param name="baseConfigurationName">The name of a base configuration to current configure DI.</param>
        /// <returns>DI configuration.</returns>
        IConfiguration DependsOn(string baseConfigurationName);

        /// <summary>
        /// Determines a custom attribute overriding an injection type.
        /// </summary>
        /// <param name="typeArgumentPosition">The optional position of a type parameter in the attribute constructor. See the predefined <see cref="TypeAttribute{T}"/> attribute.</param>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <returns>DI configuration.</returns>
        IConfiguration TypeAttribute<T>(int typeArgumentPosition = 0) where T : Attribute;

        /// <summary>
        /// Determines a tag attribute overriding an injection tag.
        /// </summary>
        /// <param name="tagArgumentPosition">The optional position of a tag parameter in the attribute constructor. See the predefined <see cref="TagAttribute{T}"/> attribute.</param>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <returns>DI configuration.</returns>
        IConfiguration TagAttribute<T>(int tagArgumentPosition = 0) where T : Attribute;

        /// <summary>
        /// Determines a custom attribute overriding an injection order. 
        /// </summary>
        /// <param name="orderArgumentPosition">The optional position of an order parameter in the attribute constructor. 0 by default. See the predefined <see cref="OrderAttribute{T}"/> attribute.</param>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <returns>DI configuration.</returns>
        IConfiguration OrderAttribute<T>(int orderArgumentPosition = 0) where T : Attribute;

        /// <summary>
        /// Overrides a default <see cref="Lifetime"/>. <see cref="Lifetime.Transient"/> is default lifetime.
        /// </summary>
        /// <param name="lifetime">The default lifetime.</param>
        /// <returns>DI configuration.</returns>
        IConfiguration Default(Lifetime lifetime);
    }

    /// <summary>
    /// The abstraction to configure a binding.
    /// </summary>
    internal interface IBinding
    {
        /// <summary>
        /// Continue a binding configuration chain, determining an additional type of a dependency.
        /// </summary>
        /// <typeparam name="T">The type of dependency to bind. Also supports generic type markers like <see cref="TT"/>, <see cref="TTList{T}"/> and others.</typeparam>
        /// <returns>Binding configuration.</returns>
        IBinding Bind<T>();

        /// <summary>
        /// Determines a binding lifetime.
        /// </summary>
        /// <param name="lifetime">The binding lifetime.</param>
        /// <returns>Binding configuration.</returns>
        IBinding As(Lifetime lifetime);

        /// <summary>
        /// Determines a binding tag.
        /// </summary>
        /// <param name="tag">The binding tag.</param>
        /// <returns>Binding configuration.</returns>
        IBinding Tag(object tag);

        /// <summary>
        /// Determines a binding suitable for any tag.
        /// </summary>
        /// <returns>Binding configuration.</returns>
        IBinding AnyTag();

        /// <summary>
        /// Finish a binding configuration chain by determining a binding implementation.
        /// </summary>
        /// <typeparam name="T">The type of binding implementation. Also supports generic type markers like <see cref="TT"/>, <see cref="TTList{T}"/> and others.</typeparam>
        /// <returns>DI configuration.</returns>
        IConfiguration To<T>();

        /// <summary>
        /// Finish a binding configuration chain by determining a binding implementation using a factory method. It allows to resole an instance manually, invoke required methods, initialize properties, fields and etc.
        /// </summary>
        /// <param name="factory">The method providing an dependency implementation.</param>
        /// <typeparam name="T">The type of binding implementation. Also supports generic type markers like <see cref="TT"/>, <see cref="TTList{T}"/> and others.</typeparam>
        /// <returns>DI configuration.</returns>
        IConfiguration To<T>(Func<IContext, T> factory);
    }

    /// <summary>
    /// The abstraction to resolve a DI dependency via <see cref="IBinding.To{T}(System.Func{Pure.DI.IContext,T})"/>.
    /// </summary>
    internal interface IContext
    {
        /// <summary>
        /// Resolves a composition root.
        /// </summary>
        /// <typeparam name="T">The type of a root.</typeparam>
        /// <returns>A resolved dependency.</returns>
        T Resolve<T>();

        /// <summary>
        /// Resolves a composition root marked with a tag. See also <see cref="IBinding.Tag"/>./>
        /// </summary>
        /// <param name="tag">The tag of of a composition root instance.</param>
        /// <typeparam name="T">The type of a composition root instance.</typeparam>
        /// <returns>A resolved dependency.</returns>
        T Resolve<T>(object tag);
    }

    /// <summary>
    /// The abstraction to intercept a resolving of a dependency of the type T. See also <seealso href="https://github.com/DevTeam/Pure.DI#intercept-specific-types-">this sample</seealso>. Besides that, it could be used to create a custom <see cref="Lifetime"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IFactory<T>
    {
        /// <summary>
        /// Provides an instance.
        /// </summary>
        /// <param name="factory">The method resolving an instance of dependency.</param>
        /// <typeparam name="T">The type of an instance.</typeparam>
        /// <returns>A resolved instance.</returns>
        T Create(Func<T> factory);
    }

    /// <summary>
    /// The abstraction to intercept a resolving of a dependency of the type T. See also <seealso href="https://github.com/DevTeam/Pure.DI#intercept-a-set-of-types-">this sample</seealso>. Besides that, it could be used to create a custom <see cref="Lifetime"/>.
    /// </summary>
    internal interface IFactory
    {
        /// <summary>
        /// Intercepts a resolving of a dependency of the type T using a method <paramref name="factory"/> for a <paramref name="tag"/>.
        /// </summary>
        /// <param name="factory">The method resolving an instance of dependency.</param>
        /// <param name="tag"></param>
        /// <typeparam name="T">The type of an instance.</typeparam>
        /// <returns>A resolved instance.</returns>
        T Create<T>(Func<T> factory, object tag);
    }

    /// <summary>
    /// Represents an event rising after an instance of the <see cref="IDisposable"/> type is created during a DI composition. Use this event to manage disposable instances: <example>Composer.OnDisposable += e => disposables.Add(e.Disposable);</example>.
    /// </summary>
    internal struct RegisterDisposableEvent
    {
        /// <summary>
        /// The <see cref="IDisposable"/> instance.
        /// </summary>
        public readonly IDisposable Disposable;

        /// <summary>
        /// A binding <see cref="Lifetime"/> relating to a disposable instance.
        /// </summary>
        public readonly Lifetime Lifetime;

        /// <summary>
        /// Creates an event instance. 
        /// </summary>
        /// <param name="disposable">The disposable instance.</param>
        /// <param name="lifetime">A binding <see cref="Lifetime"/> relating to a disposable instance.</param>
        public RegisterDisposableEvent(IDisposable disposable, Lifetime lifetime)
        {
            Disposable = disposable;
            Lifetime = lifetime;
        }
    }

    /// <summary>
    /// Represents a delegate of the <see cref="RegisterDisposableEvent"/> event rising after an instance of the <see cref="IDisposable"/> type is created during a DI composition.
    /// </summary>
    internal delegate void RegisterDisposable(RegisterDisposableEvent registerDisposableEvent);
}