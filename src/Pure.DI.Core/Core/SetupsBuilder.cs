// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class SetupsBuilder(
    Func<IMetadataSyntaxWalker> metadataSyntaxWalkerFactory,
    ICache<ImmutableArray<byte>, bool> setupCache)
    : IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>, IMetadataVisitor
{
    private readonly List<MdSetup> _setups = [];
    private readonly List<MdBinding> _bindings = [];
    private readonly List<MdRoot> _roots = [];
    private readonly List<MdDependsOn> _dependsOn = [];
    private readonly List<MdTypeAttribute> _typeAttributes = [];
    private readonly List<MdTagAttribute> _tagAttributes = [];
    private readonly List<MdOrdinalAttribute> _ordinalAttributes = [];
    private readonly List<MdUsingDirectives> _usingDirectives = [];
    private BindingBuilder _bindingBuilder = new();
    private MdSetup? _setup;

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

    public void VisitResolve(MdResolver resolver)
    {
    }

    public void VisitRoot(in MdRoot root) => _roots.Add(root);

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

    public void VisitFinish() => FinishSetup();

    private void FinishBinding()
    {
        _bindings.Add(_bindingBuilder.Build(_setup!));
    }

    private void FinishSetup()
    {
        if (_setup is not { } setup)
        {
            return;
        }

        _setups.Add(
            setup with
            {
                Bindings = _bindings.Select(i => i with { SourceSetup = setup }).ToImmutableArray(),
                Roots = _roots.ToImmutableArray(),
                DependsOn = _dependsOn.ToImmutableArray(),
                TypeAttributes = _typeAttributes.ToImmutableArray(),
                TagAttributes = _tagAttributes.ToImmutableArray(),
                OrdinalAttributes = _ordinalAttributes.ToImmutableArray(),
                UsingDirectives = _usingDirectives.ToImmutableArray()
        });
            
        _bindings.Clear();
        _roots.Clear();
        _dependsOn.Clear();
        _typeAttributes.Clear();
        _ordinalAttributes.Clear();
        _usingDirectives.Clear();
        _setup = default;
        _bindingBuilder = new BindingBuilder();
    }
}