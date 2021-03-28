namespace Pure.DI
{
    public interface IConfiguration
    {
        IBinding Bind<T>();
    }
}
