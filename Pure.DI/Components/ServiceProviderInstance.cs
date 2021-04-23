// ReSharper disable UnusedType.Global
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable StaticMemberInGenericType
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable StructCanBeMadeReadOnly
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
#pragma warning disable 649
#pragma warning disable 8618
#pragma warning disable 8600
#pragma warning disable 8603
#pragma warning disable 0436
namespace Pure.DI.Components
{
    internal struct ServiceProviderInstance
    {
        [System.ThreadStatic] public static System.IServiceProvider ServiceProvider;
    }

    internal struct ServiceProviderInstance<T>
    {
        public T Value => ServiceProviderInstance.ServiceProvider != null ? (T)ServiceProviderInstance.ServiceProvider.GetService(typeof(T)) : throw new System.InvalidOperationException("Cannot resolve an instance outside a container.");
    }
}
