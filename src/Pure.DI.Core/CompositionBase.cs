// ReSharper disable UnusedMember.Local
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI;

using System.Text.RegularExpressions;

// ReSharper disable once PartialTypeWithSinglePart
internal partial class CompositionBase
{
    private static void Setup() => DI.Setup(nameof(CompositionBase))
        .Bind<ICache<TT1, TT2>>().As(Lifetime.Singleton).To<Cache<TT1, TT2>>()
        .Bind<IResources>().As(Lifetime.PerResolve).To<Resources>()
        .Bind<Func<string, Regex>>().As(Lifetime.PerResolve).To(_ => new Func<string, Regex>(value => new Regex(value, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase)))
        .Bind<IBuilder<Unit, IEnumerable<Source>>>().To<ApiBuilder>()
        .Root<IBuilder<Unit, IEnumerable<Source>>>("ApiBuilder");
}