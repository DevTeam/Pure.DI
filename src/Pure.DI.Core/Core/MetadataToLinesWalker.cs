namespace Pure.DI.Core;

internal sealed class MetadataToLinesWalker(int indent)
    : MetadataWalkerBase, IEnumerable<string>
{
    private readonly LinesBuilder _lb = new(indent);

    public override void VisitSetup(in MdSetup setup)
    {
        foreach (var setting in setup.Hints)
        {
            _lb.AppendLine($"// {setting.Key} = {setting.Value}{Environment.NewLine}");
        }

        _lb.AppendLine($"DI.Setup(\"{setup.Name.FullName}\")");
        using (_lb.Indent())
        {
            base.VisitSetup(in setup);
        }
    }

    public override void VisitContract(in MdContract contract)
    {
        _lb.Append($".Bind<{contract.ContractType}>(");
        base.VisitContract(in contract);
        _lb.Append(")");
    }

    public override void VisitImplementation(in MdImplementation implementation) => _lb.AppendLine($".{implementation.ToString()}");

    public override void VisitFactory(in MdFactory factory) => _lb.AppendLine($".{factory.ToString()}");

    public override void VisitConstruction(in MdConstruct construct) => _lb.AppendLine($".{construct.ToString()}");

    public override void VisitDefaultLifetime(in MdDefaultLifetime defaultLifetime) => _lb.Append(defaultLifetime.ToString());

    public override void VisitDependsOn(in MdDependsOn dependsOn) => _lb.Append(dependsOn.ToString());

    public override void VisitRoot(in MdRoot root) => _lb.AppendLine($".{root.ToString()}");

    public override void VisitTypeAttribute(in MdTypeAttribute typeAttribute) => _lb.AppendLine(typeAttribute.ToString());

    public override void VisitTagAttribute(in MdTagAttribute tagAttribute) => _lb.AppendLine(tagAttribute.ToString());

    public override void VisitOrdinalAttribute(in MdOrdinalAttribute ordinalAttribute) => _lb.AppendLine(ordinalAttribute.ToString());

    public override void VisitLifetime(in MdLifetime lifetime) => _lb.Append($".{lifetime.ToString()}");

    public override void VisitTag(in MdTag tag)
    {
        if (tag.Position > 0)
        {
            _lb.Append(", ");
        }

        _lb.Append(tag.ToString());
    }

    public override void VisitFinish()
    {
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // ReSharper disable once NotDisposedResourceIsReturned
    public IEnumerator<string> GetEnumerator() => _lb.GetEnumerator();
}