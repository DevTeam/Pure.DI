// ReSharper disable UnusedMember.Local
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI;

using Core;

// ReSharper disable once PartialTypeWithSinglePart
public partial class CompositionBase
{
    private static void Setup() => DI.Setup(nameof(CompositionBase))
        .Bind<ICache<TT1, TT2>>().As(Lifetime.Singleton).To<Cache<TT1, TT2>>()
        .Bind<IResources>().To<Resources>()
        .Bind<Func<string, System.Text.RegularExpressions.Regex>>().To(_ => new Func<string, System.Text.RegularExpressions.Regex>(value => new System.Text.RegularExpressions.Regex(value, System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase)))
        .Bind<IBuilder<Unit, IEnumerable<Source>>>().To<ApiBuilder>()
        .Root<IBuilder<Unit, IEnumerable<Source>>>("ApiBuilder");
}