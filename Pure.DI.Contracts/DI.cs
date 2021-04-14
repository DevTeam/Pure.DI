// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
namespace Pure.DI
{
    using System;

    public static class DI
    {
        public static IConfiguration Setup(string targetTypeName = "") => Configuration.Shared;

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
}