namespace Pure.DI.Core;

interface IVariablesWalker
{
    IVariablesWalker Initialize(ICollection<Variable> variables);

    IReadOnlyList<Variable> GetResult();

    void VisitMethod(in Unit ctx, in DpMethod method, int? position);

    void VisitProperty(in Unit ctx, in DpProperty property, int? position);

    void VisitField(in Unit ctx, in DpField field, int? position);

    void VisitConstructor(in Unit ctx, in DpMethod constructor);
}