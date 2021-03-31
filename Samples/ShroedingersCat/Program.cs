namespace ShroedingersCat
{
    using System;
    using Models;
    using Pure.DI;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        static void Main(string[] args)
        {
            DI.Setup()
                .Bind<IBox<TT>>().To<Box<TT>>()
                .Bind<ICat>().To<Cat>()
                .Bind<Program>().As(Lifetime.Singleton).To<Program>();

            var program = Resolver.Resolve<Program>();
            program.ShowBox();
        }

        private readonly IBox<ICat> _box;

        public Program(IBox<ICat> box) => _box = box;

        public void ShowBox() => Console.WriteLine(_box);
    }
}
