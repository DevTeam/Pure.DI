namespace Pure.DI
{
    using System;

    public interface IConfiguration
    {
        IBinding Bind<T>();

        IConfiguration Fallback(Func<Type, object, object> factory);

        IConfiguration DependsOn(string configurationName);
    }
}
