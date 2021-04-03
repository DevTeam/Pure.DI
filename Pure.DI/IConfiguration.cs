namespace Pure.DI
{
    using System;

    public interface IConfiguration
    {
        IBinding Bind<T>();

        IConfiguration Using(Func<Type, object, object> factory);
    }
}
