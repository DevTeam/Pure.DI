namespace Pure.DI
{
    public interface IContext
    {
        T Resolve<T>() where T: class;

        T Resolve<T>(object tag) where T : class;
    }
}
