namespace Pure.DI.Core
{
    using System;

    internal class Configuration: IConfiguration
    {
        public static readonly IConfiguration Shared = new Configuration();

        public IBinding Bind<T>() => new Binding(this);

        public IConfiguration Fallback(Func<Type, object, object> factory) => this;
    }
}