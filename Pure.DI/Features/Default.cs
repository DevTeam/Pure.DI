// ReSharper disable PartialTypeWithSinglePart
namespace NS35EBD81B.Features
{
    using System;
    using NS35EBD81B;

    // ReSharper disable once UnusedType.Global
    internal static class Default
    {
        private static void Setup()
        {
            NS35EBD81B.DI.Setup("DefaultFeature")
                .TypeAttribute<NS35EBD81B.TypeAttribute>()
                .TagAttribute<NS35EBD81B.TagAttribute>()
                .OrderAttribute<NS35EBD81B.OrderAttribute>()

                .Bind<System.Func<TT>>()
                    .AnyTag()
                    .To(ctx => new System.Func<TT>(() => ctx.Resolve<TT>()))
                .Bind<System.Lazy<TT>>()
                    .AnyTag()
                    .To(ctx => new System.Lazy<TT>(ctx.Resolve<Func<TT>>(), true))
                .Bind<System.Lazy<TT, TT1>>()
                    .AnyTag()
                    .To(ctx => new System.Lazy<TT, TT1>(ctx.Resolve<Func<TT>>(), ctx.Resolve<TT1>(), true))
                .Bind<System.Threading.Tasks.Task<TT>>()
                    .AnyTag()
                    .To<System.Threading.Tasks.Task<TT>>()
                
                // Collections
                .Bind<System.Collections.Generic.ICollection<TT>>()
                .Bind<System.Collections.Generic.IReadOnlyCollection<TT>>()
                    .To(ctx => ctx.Resolve<TT[]>())
                .Bind<System.Collections.Generic.IList<TT>>()
                .Bind<System.Collections.Generic.List<TT>>()
                .Bind<System.Collections.Generic.IReadOnlyList<TT>>()
                    .To<System.Collections.Generic.List<TT>>()
                .Bind<System.Collections.Generic.ISet<TT>>()
                .Bind<System.Collections.Generic.HashSet<TT>>()
                    .To<System.Collections.Generic.HashSet<TT>>()
                .Bind<System.Collections.Generic.SortedSet<TT>>()
                    .To<System.Collections.Generic.SortedSet<TT>>()
                .Bind<System.Collections.Generic.Queue<TT>>()
                    .To<System.Collections.Generic.Queue<TT>>()
                .Bind<System.Collections.Generic.Stack<TT>>()
                    .To<System.Collections.Generic.Stack<TT>>()
                .Bind<System.Collections.Immutable.ImmutableArray<TT>>()
                    .To(ctx => System.Collections.Immutable.ImmutableArray.Create<TT>(ctx.Resolve<TT[]>()))
                .Bind<System.Collections.Immutable.IImmutableList<TT>>()
                .Bind<System.Collections.Immutable.ImmutableList<TT>>()
                    .To(ctx => System.Collections.Immutable.ImmutableList.Create<TT>(ctx.Resolve<TT[]>()))
                .Bind<System.Collections.Immutable.IImmutableSet<TT>>()
                .Bind<System.Collections.Immutable.ImmutableHashSet<TT>>()
                    .To(ctx => System.Collections.Immutable.ImmutableHashSet.Create<TT>(ctx.Resolve<TT[]>()))
                .Bind<System.Collections.Immutable.ImmutableSortedSet<TT>>()
                    .To(ctx => System.Collections.Immutable.ImmutableSortedSet.Create<TT>(ctx.Resolve<TT[]>()))
                .Bind<System.Collections.Immutable.IImmutableQueue<TT>>()
                .Bind<System.Collections.Immutable.ImmutableQueue<TT>>()
                    .To(ctx => System.Collections.Immutable.ImmutableQueue.Create<TT>(ctx.Resolve<TT[]>()))
                .Bind<System.Collections.Immutable.IImmutableStack<TT>>()
                .Bind<System.Collections.Immutable.ImmutableStack<TT>>()
                    .To(ctx => System.Collections.Immutable.ImmutableStack.Create<TT>(ctx.Resolve<TT[]>()));
        }
    }
}
