namespace ShroedingersCat
{
    using Models;
    using Pure.DI;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        static void Main(string[] args)
        {
            DI.Setup()
                .Bind<ICat>().As(Lifetime.Transient).To<Cat>();

            var a = CompositionRoot.Resolve<ICat>();
        }
    }
}
