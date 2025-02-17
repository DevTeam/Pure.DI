// ReSharper disable InvertIf
// ReSharper disable RedundantJumpStatement
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

sealed class VariableCodeBuilder(
    ICodeBuilder<DpImplementation> implementationBuilder,
    ICodeBuilder<DpFactory> factoryBuilder,
    ICodeBuilder<DpConstruct> constructBuilder)
    : DependenciesWalker<BuildContext>, ICodeBuilder<Variable>
{
    public void Build(BuildContext ctx, in Variable variable) =>
        VisitDependencyNode(ctx, variable.Node);

    public override void VisitImplementation(in BuildContext ctx, in DpImplementation implementation)
    {
        implementationBuilder.Build(ctx, implementation);
        base.VisitImplementation(ctx, in implementation);
    }

    public override void VisitFactory(in BuildContext ctx, in DpFactory factory)
    {
        factoryBuilder.Build(ctx, factory);
        base.VisitFactory(ctx, in factory);
    }

    public override void VisitConstruct(in BuildContext ctx, DpConstruct construct)
    {
        constructBuilder.Build(ctx, construct);
        base.VisitConstruct(ctx, construct);
    }
}