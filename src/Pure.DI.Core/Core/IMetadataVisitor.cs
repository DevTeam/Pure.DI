// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.Core;

internal interface IMetadataVisitor
{
    void VisitSetup(in MdSetup setup);
    
    void VisitUsingDirectives(in MdUsingDirectives usingDirectives);
    
    void VisitBinding(in MdBinding binding);

    void VisitContract(in MdContract contract);
    
    void VisitImplementation(in MdImplementation implementation);

    void VisitFactory(in MdFactory factory);

    void VisitResolve(MdResolver resolver);
    
    void VisitDefaultLifetime(in MdDefaultLifetime defaultLifetime);
    
    void VisitDependsOn(in MdDependsOn dependsOn);
    
    void VisitArg(in MdArg arg);
    
    void VisitRoot(in MdRoot root);

    void VisitTypeAttribute(in MdTypeAttribute typeAttribute);
    
    void VisitTagAttribute(in MdTagAttribute tagAttribute);
    
    void VisitOrdinalAttribute(in MdOrdinalAttribute ordinalAttribute);
    
    void VisitLifetime(in MdLifetime lifetime);

    void VisitTag(in MdTag tag);

    void VisitAccumulator(in MdAccumulator accumulator);

    void VisitFinish();
}