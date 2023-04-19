namespace Pure.DI.Core;

internal class MetadataVisitorBase: IMetadataVisitor
{
    public virtual void VisitSetup(in MdSetup setup)
    {
    }

    public virtual void VisitBinding(in MdBinding binding)
    {
    }

    public virtual void VisitContract(in MdContract contract)
    {
    }

    public virtual void VisitImplementation(in MdImplementation implementation)
    {
    }

    public virtual void VisitFactory(in MdFactory factory)
    {
    }

    public virtual void VisitResolve(MdResolver resolver)
    {
    }

    public virtual void VisitDefaultLifetime(in MdDefaultLifetime defaultLifetime)
    {
    }

    public virtual void VisitDependsOn(in MdDependsOn dependsOn)
    {
    }

    public virtual void VisitArg(in MdArg arg)
    {
    }

    public virtual void VisitRoot(in MdRoot root)
    {
    }

    public virtual void VisitTypeAttribute(in MdTypeAttribute typeAttribute)
    {
    }

    public virtual void VisitTagAttribute(in MdTagAttribute tagAttribute)
    {
    }

    public virtual void VisitOrdinalAttribute(in MdOrdinalAttribute ordinalAttribute)
    {
    }

    public virtual void VisitLifetime(in MdLifetime lifetime)
    {
    }

    public virtual void VisitTag(in MdTag tag)
    {
    }

    public virtual void VisitFinish()
    {
    }
}