namespace ShroedingersCat
{
    using System;
    using Models;
    using Pure.DI;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        static void Main()
        {
            DI.Setup()
                .Bind<IBox<TT>>().To<Box<TT>>()
                .Bind<string>().Tag("CatName").To(ctx => "Shroedingers")
                .Bind<string>().To(ctx => $"{ctx.Resolve<string>("CatName")} cat")
                .Bind<ICat>().To<Cat>()
                .Bind<Program>().As(Lifetime.Singleton).To<Program>();

            Resolver.Resolve<Program>().ShowBox();
        }

        private readonly IBox<ICat> _box;

        public Program(IBox<ICat> box) => _box = box;

        private void ShowBox() => Console.WriteLine(_box);
    }
}
