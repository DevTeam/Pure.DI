// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class RunInContextMembersBuilder : IMembersBuilder
{
    private readonly IArgumentsSupport _argumentsSupport;
    private readonly IStatementsBlockWrapper[] _statementsBlockWrappers;

    public RunInContextMembersBuilder(
        IArgumentsSupport argumentsSupport,
        IStatementsBlockWrapper[] statementsBlockWrappers)
    {
        _argumentsSupport = argumentsSupport;
        _statementsBlockWrappers = statementsBlockWrappers;
    }

    public int Order => 0;
    
    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
    {
        var body = SyntaxRepo.ResolveInContextMethodSyntax.Body;
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var statementsBlockWrapper in _statementsBlockWrappers)
        {
            body = statementsBlockWrapper.AddFinalizationStatements(body);
        }

        yield return
            SyntaxRepo.ResolveInContextMethodSyntax
                .AddParameterListParameters(_argumentsSupport.GetParameters().ToArray())
                .WithBody(body);
    }
    
    public static T ResolveInContext<T>(Func<T> run)
    {
        return run();
    }
}