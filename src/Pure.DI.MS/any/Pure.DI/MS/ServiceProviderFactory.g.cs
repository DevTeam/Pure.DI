// <auto-generated/>
#pragma warning disable

namespace Pure.DI.MS
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    /// <summary>
    /// A base class for a composition that can be used as a service provider factory <see cref="Microsoft.Extensions.DependencyInjection.IServiceProviderFactory{TContainerBuilder}"/>.
    /// <example>
    /// For example:
    /// <code>
    /// internal partial class Composition: ServiceProviderFactory&lt;Composition&gt;
    /// {
    ///     void Setup() =&gt;
    ///         DI.Setup(nameof(Composition))
    ///             .DependsOn(Base)
    ///             .Root&lt;HomeController&gt;();
    /// }
    /// </code>
    /// </example> 
    /// </summary>
    /// <typeparam name="TComposition"></typeparam>
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    public class ServiceProviderFactory<TComposition>: IServiceProviderFactory<IServiceCollection>
        where TComposition: ServiceProviderFactory<TComposition>
    {
        private static readonly Type KeyedServiceProviderType = Type.GetType("Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider, Microsoft.Extensions.DependencyInjection.Abstractions, Culture=neutral, PublicKeyToken=adb9793829ddae60", false);
        private static readonly MethodInfo GetKeyedServiceMethod = KeyedServiceProviderType?.GetMethod("GetKeyedService");
        private static readonly ParameterExpression TypeParameter = Expression.Parameter(typeof(Type));
        private static readonly ParameterExpression TagParameter = Expression.Parameter(typeof(object));
    
        /// <summary>
        /// The name of the Pure.DI setup to use as a dependency in other setups.
        /// <example>
        /// For example:
        /// <code>
        /// void Setup() =&amp;gt;
        ///     DI.Setup(nameof(Composition)).DependsOn(Base);
        /// </code>
        /// </example>
        /// </summary>
        protected const string Base = "Pure.DI.MS.ServiceProviderFactory";
    
        /// <summary>
        /// An instance of <see cref="Pure.DI.MS.ServiceCollectionFactory"/>.
        /// </summary>
        private static readonly ServiceCollectionFactory<TComposition> ServiceCollectionFactory = new ServiceCollectionFactory<TComposition>();
    
        /// <summary>
        /// <see cref="System.IServiceProvider"/> instance for resolving external dependencies.
        /// </summary>
        private volatile Func<Type, object, object?>? _serviceProvider;
    
        /// <summary>
        /// DI setup hints.
        /// </summary>
        [global::System.Diagnostics.Conditional("A2768DE22DE3E430C9653990D516CC9B")]
        private static void HintsSetup() =>
            global::Pure.DI.DI.Setup(Base, global::Pure.DI.CompositionKind.Internal)
                .Hint(global::Pure.DI.Hint.OnCannotResolve, "On")
                .Hint(global::Pure.DI.Hint.OnCannotResolvePartial, "Off")
                .Hint(global::Pure.DI.Hint.OnNewRoot, "On")
                .Hint(global::Pure.DI.Hint.OnNewRootPartial, "Off")
                // Specifies not to attempt to resolve types whose fully qualified name
                // begins with Microsoft.Extensions, Microsoft.AspNetCore
                // since ServiceProvider will be used to retrieve them.
                .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "Microsoft.Extensions.*")
                .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "Microsoft.AspNetCore.*");

        /// <summary>
        /// Creates a service collection <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollection"/> based on the registered composition.
        /// </summary>
        /// <param name="composition">An instance of composition.</param>
        /// <returns>An instance of <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollection"/>.</returns>
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
        [global::System.Diagnostics.Contracts.Pure]
#endif
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected static IServiceCollection CreateServiceCollection(TComposition composition)
        {
            return ServiceCollectionFactory.CreateServiceCollection(composition);
        }

        /// <inheritdoc />
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            // Registers composition roots as services in the service collection.
            return services.Add(CreateServiceCollection((TComposition)this));
        }

        /// <inheritdoc />
        public IServiceProvider CreateServiceProvider(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            _serviceProvider ??= 
                KeyedServiceProviderType?.IsAssignableFrom(serviceProvider.GetType()) == true
                    ? Expression.Lambda<Func<Type, object, object>>(
                        Expression.Call(Expression.Constant(serviceProvider), GetKeyedServiceMethod, TypeParameter, TagParameter),
                        TypeParameter,
                        TagParameter).Compile()
                    : new Func<Type, object, object>((type, tag) => serviceProvider.GetService(type));
            return serviceProvider;
        }

        /// <summary>
        /// Used to resolve external dependencies using the service provider <see cref="System.IServiceProvider"/>.
        /// </summary>
        /// <param name="tag">Dependency resolution tag.</param>
        /// <param name="lifetime">Dependency resolution lifetime.</param>
        /// <typeparam name="T">Dependency resolution type.</typeparam>
        /// <returns>Resolved dependency instance.</returns>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal T OnCannotResolve<T>(object? tag, Lifetime lifetime)
        {
            return (T)(_serviceProvider ?? throw new InvalidOperationException("Not ready yet."))(typeof(T), tag)
                   ?? throw new InvalidOperationException($"No composition root or service is registered for type {typeof(T)}{(tag == null ? "" : $"({tag})")}.");
        }

        /// <summary>
        /// Registers a composition resolver for use in a service collection <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollection"/>.
        /// </summary>
        /// <param name="resolver">Instance resolver.</param>
        /// <param name="name">The name of the composition root.</param>
        /// <param name="tag">The tag of the composition root.</param>
        /// <param name="lifetime">The lifetime of the composition root.</param>
        /// <typeparam name="TContract">The contract type of the composition root.</typeparam>
        /// <typeparam name="T">The implementation type of the composition root.</typeparam>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static void OnNewRoot<TContract, T>(
            IResolver<TComposition, TContract> resolver,
            string name, object tag, Lifetime lifetime)
        {
            ServiceCollectionFactory.AddResolver(resolver, tag);
        }
    }
#pragma warning restore
}