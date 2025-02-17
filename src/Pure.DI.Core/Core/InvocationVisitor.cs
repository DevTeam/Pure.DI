namespace Pure.DI.Core;

using System.Runtime.CompilerServices;

readonly record struct InvocationVisitor(
    SemanticModel SemanticModel,
    InvocationExpressionSyntax Invocation,
    IMetadataVisitor BaseVisitor,
    CancellationToken CancellationToken)
    : IMetadataVisitor
{
    private readonly List<IRunnable> _actions = [];

    public void VisitSetup(in MdSetup setup) =>
        AddAction((visitor, i) => visitor.VisitSetup(i), BaseVisitor, setup);

    public void VisitUsingDirectives(in MdUsingDirectives usingDirectives) =>
        AddAction((visitor, i) => visitor.VisitUsingDirectives(i), BaseVisitor, usingDirectives);

    public void VisitBinding(in MdBinding binding) =>
        AddAction((visitor, i) => visitor.VisitBinding(i), BaseVisitor, binding);

    public void VisitContract(in MdContract contract) =>
        AddAction((visitor, i) => visitor.VisitContract(i), BaseVisitor, contract);

    public void VisitImplementation(in MdImplementation implementation) =>
        AddAction((visitor, i) => visitor.VisitImplementation(i), BaseVisitor, implementation);

    public void VisitFactory(in MdFactory factory) =>
        AddAction((visitor, i) => visitor.VisitFactory(i), BaseVisitor, factory);

    public void VisitResolver(in MdResolver resolver) =>
        AddAction((visitor, i) => visitor.VisitResolver(i), BaseVisitor, resolver);

    public void VisitInitializer(MdInitializer initializer) =>
        AddAction((visitor, i) => visitor.VisitInitializer(i), BaseVisitor, initializer);

    public void VisitDefaultLifetime(in MdDefaultLifetime defaultLifetime) =>
        AddAction((visitor, i) => visitor.VisitDefaultLifetime(i), BaseVisitor, defaultLifetime);

    public void VisitDependsOn(in MdDependsOn dependsOn) =>
        AddAction((visitor, i) => visitor.VisitDependsOn(i), BaseVisitor, dependsOn);

    public void VisitArg(in MdArg arg) =>
        AddAction((visitor, i) => visitor.VisitArg(i), BaseVisitor, arg);

    public void VisitRoot(in MdRoot root) =>
        AddAction((visitor, i) => visitor.VisitRoot(i), BaseVisitor, root);

    public void VisitGenericTypeArgument(in MdGenericTypeArgument genericTypeArgument) =>
        AddAction((visitor, i) => visitor.VisitGenericTypeArgument(i), BaseVisitor, genericTypeArgument);

    public void VisitGenericTypeArgumentAttribute(in MdGenericTypeArgumentAttribute genericTypeArgumentAttribute) =>
        AddAction((visitor, i) => visitor.VisitGenericTypeArgumentAttribute(i), BaseVisitor, genericTypeArgumentAttribute);

    public void VisitTypeAttribute(in MdTypeAttribute typeAttribute) =>
        AddAction((visitor, i) => visitor.VisitTypeAttribute(i), BaseVisitor, typeAttribute);

    public void VisitTagAttribute(in MdTagAttribute tagAttribute) =>
        AddAction((visitor, i) => visitor.VisitTagAttribute(i), BaseVisitor, tagAttribute);

    public void VisitOrdinalAttribute(in MdOrdinalAttribute ordinalAttribute) =>
        AddAction((visitor, i) => visitor.VisitOrdinalAttribute(i), BaseVisitor, ordinalAttribute);

    public void VisitLifetime(in MdLifetime lifetime) =>
        AddAction((visitor, i) => visitor.VisitLifetime(i), BaseVisitor, lifetime);

    public void VisitTag(in MdTag tag) =>
        AddAction((visitor, i) => visitor.VisitTag(i), BaseVisitor, tag);

    public void VisitAccumulator(in MdAccumulator accumulator) =>
        AddAction((visitor, i) => visitor.VisitAccumulator(i), BaseVisitor, accumulator);

    public void VisitHint(in MdHint hint) =>
        AddAction((visitor, i) => visitor.VisitHint(i), BaseVisitor, hint);

    public void VisitFinish() =>
        AddAction((visitor, _) => visitor.VisitFinish(), BaseVisitor, 0);

    public void Apply()
    {
        foreach (var action in _actions)
        {
            CancellationToken.ThrowIfCancellationRequested();
            action.Run();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddAction<T>(Action<IMetadataVisitor, T> action, IMetadataVisitor visitor, in T state) =>
        _actions.Add(new VisitorAction<T>(action, visitor, state));

    private interface IRunnable
    {
        void Run();
    }

    private class VisitorAction<T>(
        Action<IMetadataVisitor, T> action,
        IMetadataVisitor visitor,
        T state)
        : IRunnable
    {
        public void Run() => action(visitor, state);
    }
}