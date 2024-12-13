// ReSharper disable UnusedType.Global
namespace Pure.DI.Benchmarks.Containers;

using global::SimpleInjector;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class SimpleInjector : BaseAbstractContainer<Container>
{
    private readonly Container _container = new();
    private readonly Lazy<Container> _containerProvider;
    private readonly Dictionary<Type, List<Type>> _collections = new();

    public SimpleInjector()
    {
        _container.Options.EnableAutoVerification = false;
        _container.Options.UseStrictLifestyleMismatchBehavior = false;
        _container.Options.SuppressLifestyleMismatchVerification = true;

        _containerProvider = new Lazy<Container>(() =>
        {
            foreach (var (contractType, implementations) in _collections.Where(i => i.Value.Count > 1))
            {
                _container.Collection.Register(contractType, implementations);
            }

            return _container;
        });
    }

    public override Container CreateContainer() => _containerProvider.Value;

    public override IAbstractContainer<Container> Bind(
        Type contractType,
        Type implementationType,
        AbstractLifetime lifetime = AbstractLifetime.Transient,
        string? name = null)
    {
        switch (lifetime)
        {
            case AbstractLifetime.Transient:
                if (!_collections.TryGetValue(contractType, out var implementations))
                {
                    implementations = [implementationType];
                    _collections.Add(contractType, implementations);
                    _container.Register(contractType, implementationType);
                }
                else
                {
                    implementations.Add(contractType);
                }

                break;

            case AbstractLifetime.Singleton:
                _container.RegisterSingleton(contractType, implementationType);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }

        return this;
    }

    public override T Resolve<T>() where T : class => _container.GetInstance<T>();

    public override void Dispose() => _container.Dispose();
}