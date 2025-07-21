namespace Pure.DI.Core.Code;

interface IVariablesWalker
{
    IReadOnlyList<VarInjection> GetResult();

    void VisitMethod(in Unit ctx, in DpMethod method, int? position);

    void VisitProperty(in Unit ctx, in DpProperty property, int? position);

    void VisitField(in Unit ctx, in DpField field, int? position);

    void VisitConstructor(in Unit ctx, in DpMethod constructor);
}