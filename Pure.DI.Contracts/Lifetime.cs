// ReSharper disable UnusedMember.Global
namespace Pure.DI
{
    public enum Lifetime
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
}