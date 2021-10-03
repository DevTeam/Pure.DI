// ReSharper disable PartialTypeWithSinglePart
namespace Pure.DI.Features
{
    // ReSharper disable once UnusedType.Global
    internal static partial class Default
    {
        private static void Setup()
        {
            DI.Setup("DefaultFeature")
                .TypeAttribute<TypeAttribute>()
                .TagAttribute<TagAttribute>()
                .OrderAttribute<OrderAttribute>()

                .Bind<System.Func<TT>>()
                    .AnyTag()
                    .To(ctx => new System.Func<TT>(ctx.Resolve<TT>))
                .Bind<System.Lazy<TT>>()
                    .AnyTag()
                    .To(ctx => new System.Lazy<TT>(ctx.Resolve<System.Func<TT>>(), true))
                .Bind<System.Threading.Tasks.Task<TT>>()
                    .AnyTag()
                    .To(ctx => new System.Threading.Tasks.Task<TT>(ctx.Resolve<TT>))
                
                // Enumerable
                .Bind<System.Collections.Generic.ICollection<TT>>()
                .Bind<System.Collections.Generic.IReadOnlyCollection<TT>>()
                    .To(ctx => ctx.Resolve<TT[]>())
                .Bind<System.Collections.Generic.IList<TT>>()
                .Bind<System.Collections.Generic.IReadOnlyList<TT>>()
                    .To(ctx => new System.Collections.Generic.List<TT>(ctx.Resolve<TT[]>()))
                .Bind<System.Collections.Generic.ISet<TT>>()
                    .To(ctx => new System.Collections.Generic.HashSet<TT>(ctx.Resolve<TT[]>()));
        }
    }
}
