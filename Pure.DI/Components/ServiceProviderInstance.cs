// ReSharper disable UnusedType.Global
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable StaticMemberInGenericType
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable StructCanBeMadeReadOnly
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable ArrangeAccessorOwnerBody
#pragma warning disable 649
#pragma warning disable 8618
#pragma warning disable 8600
#pragma warning disable 8603
#pragma warning disable 0436
namespace Pure.DI
{
#if NETSTANDARD1_6 || NETSTANDARD1_5 || NETSTANDARD1_4 || NETSTANDARD1_3 || NETSTANDARD1_2 || NETSTANDARD1_1 || NETSTANDARD1_0
// ReSharper disable UnusedType.Global
    internal interface IServiceProvider
    {
        object GetService(System.Type serviceType);
    }
#endif
    
    internal struct ServiceProviderInstance
    {
        [System.ThreadStatic] public static
#if NETSTANDARD1_6 || NETSTANDARD1_5 || NETSTANDARD1_4 || NETSTANDARD1_3 || NETSTANDARD1_2 || NETSTANDARD1_1 || NETSTANDARD1_0
            Pure.DI.IServiceProvider
#else
            System.IServiceProvider
#endif
            ServiceProvider;
    }

    internal struct ServiceProviderInstance<T>
    {
        public T Value
        {
            get
            {
                if (ServiceProviderInstance.ServiceProvider != null)
                {
                    return (T) ServiceProviderInstance.ServiceProvider.GetService(typeof(T));
                }

                throw new System.InvalidOperationException("Cannot resolve an instance outside a container.");
            }
        }
    }
}