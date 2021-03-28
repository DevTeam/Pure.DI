namespace Pure.DI
{
    public interface IBinding
    {
        IBinding Bind<T>();

        IBinding As(Lifetime lifetime);

        IBinding Tag(object tag);

        IConfiguration To<T>();
    }
}
