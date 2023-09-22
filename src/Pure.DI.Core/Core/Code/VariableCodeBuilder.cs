// ReSharper disable InvertIf
// ReSharper disable RedundantJumpStatement
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class VariableCodeBuilder : DependenciesWalker<BuildContext>, ICodeBuilder<Variable>
{
    private readonly ICodeBuilder<DpImplementation> _implementationBuilder;
    private readonly ICodeBuilder<DpFactory> _factoryBuilder;
    private readonly ICodeBuilder<DpConstruct> _constructBuilder;

    public VariableCodeBuilder(
        ICodeBuilder<DpImplementation> implementationBuilder,
        ICodeBuilder<DpFactory> factoryBuilder,
        ICodeBuilder<DpConstruct> constructBuilder)
    {
        _implementationBuilder = implementationBuilder;
        _factoryBuilder = factoryBuilder;
        _constructBuilder = constructBuilder;
    }

    public void Build(BuildContext ctx, in Variable variable) => 
        VisitDependencyNode(ctx, variable.Node);

    public override void VisitImplementation(in BuildContext ctx, in DpImplementation implementation)
    {
        _implementationBuilder.Build(ctx, implementation);
        base.VisitImplementation(ctx, in implementation);
    }

    public override void VisitFactory(in BuildContext ctx, in DpFactory factory)
    {
        _factoryBuilder.Build(ctx, factory);
        base.VisitFactory(ctx, in factory);
    }

    public override void VisitConstruct(in BuildContext ctx, DpConstruct construct)
    {
        _constructBuilder.Build(ctx, construct);
        base.VisitConstruct(ctx, construct);
    }
}