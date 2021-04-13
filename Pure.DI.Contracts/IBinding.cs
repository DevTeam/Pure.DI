// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedMember.Global
namespace Pure.DI
{
    using System;

    public interface IBinding
    {
        IBinding Bind<T>();

        IBinding As(Lifetime lifetime);

        IBinding Tag(object tag);

        IConfiguration To<T>();

        IConfiguration To<T>(Func<IContext, T> factory);
    }
}
