namespace Pure.DI.Core;

using System.Collections;

internal class DependenciesToLinesWalker: DependenciesWalker, IEnumerable<string>
{
    private readonly LinesBuilder _lb;
    
    public DependenciesToLinesWalker(int indent) => _lb = new LinesBuilder(indent);

    public override void VisitRoot(in DpRoot root)
    {
        using (_lb.Indent())
        {
            _lb.Append($"{root.Source.RootType}({root.Source.Tag?.Value ?? ""}) {root.Source.Name}");
            base.VisitRoot(in root);
        }
    }

    public override void VisitFactory(in DpFactory factory)
    {
        using (_lb.Indent())
        {
            var mdFactory = factory.Source;
            _lb.Append($"{mdFactory.Factory.Body}");
            base.VisitFactory(in factory);
        }
    }
    
    public override void VisitArg(in DpArg arg)
    {
        using (_lb.Indent())
        {
            _lb.Append($"{arg.Source.Type} {arg.Source.ArgName}");
        }
    }

    public override void VisitConstruct(DpConstruct construct)
    {
        using (_lb.Indent())
        {
            _lb.Append($"{construct.Source.Type} {construct.Source.Kind.ToString().ToLowerInvariant()} of {string.Join(", ", construct.Injections)}");
        }
    }

    public override void VisitConstructor(in DpMethod constructor)
    {
        using (_lb.Indent())
        {
            VisitMethod(constructor);
        }
    }

    public override void VisitMethod(in DpMethod method)
    {
        var typeArgs = "";
        if (method.Method.ContainingType.TypeArguments.Any())
        {
            typeArgs = $"<{string.Join(", ", method.Method.ContainingType.TypeArguments)}>";
        }
        
        _lb.Append($"{method.Method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{typeArgs}(");
        var isFirst = true;
        foreach (var parameter in method.Parameters)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                _lb.Append(", ");
            }

            VisitParameter(parameter);
        }
        
        _lb.Append(")");
    }

    public override void VisitParameter(in DpParameter parameter) => _lb.Append(parameter.ToString());

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<string> GetEnumerator() => _lb.GetEnumerator();
}