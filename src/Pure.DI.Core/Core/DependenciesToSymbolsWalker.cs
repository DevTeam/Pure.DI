namespace Pure.DI.Core;

internal sealed class DependenciesToSymbolsWalker: DependenciesWalker, IEnumerable<ISymbol>
{
    private readonly List<ISymbol> _symbols = new();

    public override void VisitParameter(in DpParameter parameter)
    {
        _symbols.Add(parameter.ParameterSymbol);
        base.VisitParameter(in parameter);
    }

    public override void VisitProperty(in DpProperty property)
    {
        _symbols.Add(property.Property);
        base.VisitProperty(in property);
    }

    public override void VisitField(in DpField field)
    {
        _symbols.Add(field.Field);
        base.VisitField(in field);
    }

    public override void VisitFactory(in DpFactory factory)
    {
        foreach (var resolver in factory.Source.Resolvers)
        {
            _symbols.Add(resolver.ContractType);
        }

        base.VisitFactory(in factory);
    }

    public IEnumerator<ISymbol> GetEnumerator() => _symbols.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}