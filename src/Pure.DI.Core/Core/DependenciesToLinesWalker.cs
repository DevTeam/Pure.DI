namespace Pure.DI.Core;

internal sealed class DependenciesToLinesWalker(int indent)
    : DependenciesWalker<Unit>, IEnumerable<string>
{
    private readonly LinesBuilder _lb = new(indent);

    public override void VisitRoot(in Unit ctx, in DpRoot root)
    {
        using (_lb.Indent())
        {
            _lb.Append($"{root.Source.RootType}({root.Source.Tag?.Value ?? ""}) {root.Source.Name}");
            base.VisitRoot(ctx, in root);
        }
    }

    public override void VisitFactory(in Unit ctx, in DpFactory factory)
    {
        using (_lb.Indent())
        {
            var mdFactory = factory.Source;
            _lb.Append($"{mdFactory.Factory.Body}");
            base.VisitFactory(ctx, in factory);
        }
    }
    
    public override void VisitArg(in Unit ctx, in DpArg arg)
    {
        using (_lb.Indent())
        {
            _lb.Append($"{arg.Source.Type} {arg.Source.ArgName}");
        }
    }

    public override void VisitConstruct(in Unit ctx, DpConstruct construct)
    {
        using (_lb.Indent())
        {
            _lb.Append($"{construct.Source.Type} {construct.Source.Kind.ToString().ToLowerInvariant()} of {string.Join(", ", construct.Injections)}");
        }
    }

    public override void VisitConstructor(in Unit ctx, in DpMethod constructor)
    {
        using (_lb.Indent())
        {
            VisitMethod(ctx, constructor);
        }
    }

    public override void VisitMethod(in Unit ctx, in DpMethod method)
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

            VisitParameter(ctx, parameter);
        }
        
        _lb.Append(")");
    }

    public override void VisitParameter(in Unit ctx, in DpParameter parameter) => _lb.Append(parameter.ToString());

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<string> GetEnumerator() => _lb.GetEnumerator();
}