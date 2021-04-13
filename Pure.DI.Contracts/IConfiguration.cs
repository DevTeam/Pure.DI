// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global
namespace Pure.DI
{
    using System;

    public interface IConfiguration
    {
        IBinding Bind<T>();

        IConfiguration Fallback(Func<Type, object, object> factory);

        IConfiguration DependsOn(string configurationName);

        IConfiguration TypeAttribute<T>(int typeArgumentPosition = 0) where T : Attribute;

        IConfiguration TagAttribute<T>(int tagArgumentPosition = 0) where T : Attribute;

        IConfiguration OrderAttribute<T>(int orderArgumentPosition = 0) where T : Attribute;
    }
}
