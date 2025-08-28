namespace EF;

using System.Diagnostics;
using Pure.DI;
using Pure.DI.MS;

partial class Composition: ServiceProviderFactory<Composition>
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .DependsOn(Base)
        .Root<Program>(nameof(Root))

        .Bind().As(Lifetime.PerResolve).To<PersonService>()
        .Bind().As(Lifetime.Singleton).To<ContactService>();
}