// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class SetupsBuilder(
    Func<IMetadataSyntaxWalker> metadataSyntaxWalkerFactory,
    ICache<ImmutableArray<byte>, bool> setupCache,
    Func<IBindingBuilder> bindingBuilderFactory)
    : IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>, IMetadataVisitor
{
    private readonly List<MdSetup> _setups = [];
    private readonly List<MdBinding> _bindings = [];
    private readonly List<MdRoot> _roots = [];
    private readonly List<MdDependsOn> _dependsOn = [];
    private readonly List<MdGenericTypeArgument> _genericTypeArguments = [];
    private readonly List<MdGenericTypeArgumentAttribute> _genericTypeArgumentAttributes = [];
    private readonly List<MdTypeAttribute> _typeAttributes = [];
    private readonly List<MdTagAttribute> _tagAttributes = [];
    private readonly List<MdOrdinalAttribute> _ordinalAttributes = [];
    private readonly List<MdUsingDirectives> _usingDirectives = [];
    private readonly List<MdAccumulator> _accumulators = [];
    private IBindingBuilder _bindingBuilder = bindingBuilderFactory();
    private MdSetup? _setup;
    private Hints _hints = new();

    public IEnumerable<MdSetup> Build(SyntaxUpdate update)
    {
        var checkSum = update.Node.SyntaxTree.GetText().GetChecksum();
        if (!setupCache.Get(checkSum, _ => true))
        {
            return Array.Empty<MdSetup>();
        }
        
        metadataSyntaxWalkerFactory().Visit(this, update);
        if (!_setups.Any())
        {
            setupCache.Set(checkSum, false);
        }
        
        return _setups;
    }

    public void VisitSetup(in MdSetup setup)
    {
        FinishSetup();
        _setup = setup;
    }

    public void VisitUsingDirectives(in MdUsingDirectives usingDirectives) =>
        _usingDirectives.Add(usingDirectives);

    public void VisitBinding(in MdBinding binding)
    {
    }

    public void VisitContract(in MdContract contract) => _bindingBuilder.AddContract(contract);

    public void VisitImplementation(in MdImplementation implementation)
    {
        _bindingBuilder.Implementation = implementation;
        FinishBinding();
    }

    public void VisitFactory(in MdFactory factory)
    {
        _bindingBuilder.Factory = factory;
        FinishBinding();
    }

    public void VisitArg(in MdArg arg)
    {
        _bindingBuilder.Arg = arg;
        FinishBinding();
    }

    public void VisitResolve(in MdResolver resolver)
    {
    }

    public void VisitRoot(in MdRoot root) => _roots.Add(root);
    
    public void VisitGenericTypeArgument(in MdGenericTypeArgument genericTypeArgument) => 
        _genericTypeArguments.Add(genericTypeArgument);

    public void VisitGenericTypeArgumentAttribute(in MdGenericTypeArgumentAttribute genericTypeArgumentAttribute) => 
        _genericTypeArgumentAttributes.Add(genericTypeArgumentAttribute);

    public void VisitDefaultLifetime(in MdDefaultLifetime defaultLifetime) =>
        _bindingBuilder.DefaultLifetime = defaultLifetime;

    public void VisitDependsOn(in MdDependsOn dependsOn) =>
        _dependsOn.Add(dependsOn);

    public void VisitTypeAttribute(in MdTypeAttribute typeAttribute) =>
        _typeAttributes.Add(typeAttribute);

    public void VisitTagAttribute(in MdTagAttribute tagAttribute) =>
        _tagAttributes.Add(tagAttribute);

    public void VisitOrdinalAttribute(in MdOrdinalAttribute ordinalAttribute) =>
        _ordinalAttributes.Add(ordinalAttribute);

    public void VisitLifetime(in MdLifetime lifetime) => 
        _bindingBuilder.Lifetime = lifetime;

    public void VisitTag(in MdTag tag) => _bindingBuilder.AddTag(tag);

    public void VisitAccumulator(in MdAccumulator accumulator) => 
        _accumulators.Add(accumulator);

    public void VisitHint(in MdHint hint) =>
        _hints[hint.Key] = hint.Value;

    public void VisitFinish() => FinishSetup();

    private void FinishBinding() => 
        _bindings.Add(_bindingBuilder.Build(_setup!));

    private void FinishSetup()
    {
        if (_setup is not { } setup)
        {
            return;
        }

        _setups.Add(
            setup with
            {
                Hints = _hints,
                Bindings = _bindings.Select(i => i with { SourceSetup = setup }).ToImmutableArray(),
                Roots = _roots.ToImmutableArray(),
                DependsOn = _dependsOn.ToImmutableArray(),
                GenericTypeArguments = _genericTypeArguments.ToImmutableArray(),
                GenericTypeArgumentAttributes = _genericTypeArgumentAttributes.ToImmutableArray(),
                TypeAttributes = _typeAttributes.ToImmutableArray(),
                TagAttributes = _tagAttributes.ToImmutableArray(),
                OrdinalAttributes = _ordinalAttributes.ToImmutableArray(),
                UsingDirectives = _usingDirectives.ToImmutableArray(),
                Accumulators = _accumulators.Distinct().ToImmutableArray()
        });

        _hints = new Hints();
        _bindings.Clear();
        _roots.Clear();
        _dependsOn.Clear();
        _genericTypeArguments.Clear();
        _genericTypeArgumentAttributes.Clear();
        _typeAttributes.Clear();
        _ordinalAttributes.Clear();
        _usingDirectives.Clear();
        _accumulators.Clear();
        _setup = default;
        _bindingBuilder = bindingBuilderFactory();
    }
}