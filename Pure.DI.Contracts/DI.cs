// ReSharper disable UnusedParameter.Global
namespace Pure.DI
{
    using System;

    public static class DI
    {
        public static IConfiguration Setup(string configurationName = "") => Configuration.Shared;

        private class Configuration : IConfiguration
        {
            public static readonly IConfiguration Shared = new Configuration();

            public IBinding Bind<T>() => new Binding(this);

            public IConfiguration Fallback(Func<Type, object, object> factory) => this;

            public IConfiguration DependsOn(string configurationName) => this;
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
}