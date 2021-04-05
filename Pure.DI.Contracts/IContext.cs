namespace Pure.DI
{
    public interface IContext
    {
        T Resolve<T>();

        T Resolve<T>(object tag);
    }
}
