// ReSharper disable PartialTypeWithSinglePart
namespace Pure.DI.Features
{
    // ReSharper disable once UnusedType.Global
    internal static partial class Default
    {
        static Default()
        {
            DI.Setup("DefaultFeature")
                .TypeAttribute<TypeAttribute>()
                .TagAttribute<TagAttribute>()
                .OrderAttribute<OrderAttribute>()

                .Bind<System.Func<TT>>().To(ctx => new System.Func<TT>(ctx.Resolve<TT>))
                .Bind<System.Threading.Tasks.Task<TT>>().To(ctx => new System.Threading.Tasks.Task<TT>(ctx.Resolve<TT>))
                
                // Enumerable
                .Bind<System.Collections.Generic.ICollection<TT>>()
                .Bind<System.Collections.Generic.IReadOnlyCollection<TT>>()
                    .To(ctx => ctx.Resolve<TT[]>())
                .Bind<System.Collections.Generic.IList<TT>>()
                .Bind<System.Collections.Generic.IReadOnlyList<TT>>()
                    .To(ctx => new System.Collections.Generic.List<TT>(ctx.Resolve<TT[]>()))
                .Bind<System.Collections.Generic.ISet<TT>>().To(ctx => new System.Collections.Generic.HashSet<TT>(ctx.Resolve<TT[]>()));
        }
    }
}
