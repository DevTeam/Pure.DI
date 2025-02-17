namespace Pure.DI.Core;

interface IVariablesWalker
{
    IVariablesWalker Initialize(ICollection<Variable> variables);

    IReadOnlyList<Variable> GetResult();

    void VisitMethod(in Unit ctx, in DpMethod method);

    void VisitProperty(in Unit ctx, in DpProperty property);

    void VisitField(in Unit ctx, in DpField field);

    void VisitConstructor(in Unit ctx, in DpMethod constructor);
}