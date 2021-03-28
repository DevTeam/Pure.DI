namespace Pure.DI.Core
{
    internal class Configuration: IConfiguration
    {
        public static readonly IConfiguration Shared = new Configuration();

        public IBinding Bind<T>() => new Binding(this);
    }
}