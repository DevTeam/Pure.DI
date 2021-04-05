// ReSharper disable PartialTypeWithSinglePart
namespace Pure.DI.Features
{
    // ReSharper disable once UnusedType.Global
    internal static partial class DefaultFeature
    {
        static DefaultFeature()
        {
            DI.Setup("DefaultFeature")
                .Bind<System.Func<TT>>().To(ctx => new System.Func<TT>(ctx.Resolve<TT>))
                .Bind<System.Lazy<TT>>().To<System.Lazy<TT>>()
                .Bind<System.Threading.ThreadLocal<TT>>().To<System.Threading.ThreadLocal<TT>>()
                .Bind<System.Threading.Tasks.Task<TT>>().To(ctx => System.Threading.Tasks.Task.Run(ctx.Resolve<System.Func<TT>>()))
                
                // Enumerable
                .Bind<TT[]>().To<TT[]>()
                .Bind<System.Collections.Generic.IEnumerable<TT>>()
                .Bind<System.Collections.Generic.ICollection<TT>>()
                .Bind<System.Collections.Generic.IReadOnlyCollection<TT>>()
                .Bind<System.Collections.Generic.IList<TT>>()
                .Bind<System.Collections.Generic.IReadOnlyList<TT>>()
                    .To(ctx => new System.Collections.Generic.List<TT>(ctx.Resolve<TT[]>()))
                .Bind<System.Collections.Generic.ISet<TT>>().To(ctx => new System.Collections.Generic.HashSet<TT>(ctx.Resolve<TT[]>()))

                // Tuples
                .Bind<System.Tuple<TT, TT1>>().To<System.Tuple<TT, TT1>>()
                .Bind<(TT, TT1)>().To<(TT, TT1)>()
                .Bind<System.Tuple<TT, TT1, TT2>>().To<System.Tuple<TT, TT1, TT2>>()
                .Bind<(TT, TT1, TT2)>().To<(TT, TT1, TT2)>()
                .Bind<System.Tuple<TT, TT1, TT2, TT3>>().To<System.Tuple<TT, TT1, TT2, TT3>>()
                .Bind<(TT, TT1, TT2, TT3)>().To<(TT, TT1, TT2, TT3)>()
                .Bind<System.Tuple<TT, TT1, TT2, TT3, TT4>>().To<System.Tuple<TT, TT1, TT2, TT3, TT4>>()
                .Bind<(TT, TT1, TT2, TT3, TT4)>().To<(TT, TT1, TT2, TT3, TT4)>()
                .Bind<System.Tuple<TT, TT1, TT2, TT3, TT4, TT5>>().To<System.Tuple<TT, TT1, TT2, TT3, TT4, TT5>>()
                .Bind<(TT, TT1, TT2, TT3, TT4, TT5)>().To<(TT, TT1, TT2, TT3, TT4, TT5)>()
                .Bind<System.Tuple<TT, TT1, TT2, TT3, TT4, TT5, TT6>>().To<System.Tuple<TT, TT1, TT2, TT3, TT4, TT5, TT6>>()
                .Bind<(TT, TT1, TT2, TT3, TT4, TT5, TT6)>().To<(TT, TT1, TT2, TT3, TT4, TT5, TT6)>()
                .Bind<System.Tuple<TT, TT1, TT2, TT3, TT4, TT5, TT6, TT7>>().To<System.Tuple<TT, TT1, TT2, TT3, TT4, TT5, TT6, TT7>>()
                .Bind<(TT, TT1, TT2, TT3, TT4, TT5, TT6, TT7)>().To<(TT, TT1, TT2, TT3, TT4, TT5, TT6, TT7)>();
        }
    }
}
