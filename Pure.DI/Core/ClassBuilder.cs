// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable IdentifierTypo
namespace Pure.DI.Core;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
// ReSharper disable once ClassNeverInstantiated.Global
internal class ClassBuilder : IClassBuilder
{
    private readonly IMembersBuilder[] _membersBuilder;
    private readonly IDiagnostic _diagnostic;
    private readonly IBindingsProbe _bindingsProbe;
    private readonly ICompilationUnitSyntaxBuilder _compilationUnitSyntaxBuilder;
    private readonly IBuildContext _buildContext;
    private readonly ResolverMetadata _metadata;

    public ClassBuilder(
        IBuildContext buildContext,
        ResolverMetadata metadata,
        IMembersBuilder[] membersBuilder,
        IDiagnostic diagnostic,
        IBindingsProbe bindingsProbe,
        ICompilationUnitSyntaxBuilder compilationUnitSyntaxBuilder)
    {
        _buildContext = buildContext;
        _metadata = metadata;
        _membersBuilder = membersBuilder;
        _diagnostic = diagnostic;
        _bindingsProbe = bindingsProbe;
        _compilationUnitSyntaxBuilder = compilationUnitSyntaxBuilder;
    }

    public CompilationUnitSyntax Build(SemanticModel semanticModel)
    {
        var classModifiers = new List<SyntaxToken>();
        _buildContext.NameService.ReserveName(_metadata.ComposerTypeName);
        if (_metadata.Owner == null)
        {
            classModifiers.Add(SyntaxKind.InternalKeyword.WithSpace());
            classModifiers.Add(SyntaxKind.StaticKeyword.WithSpace());
            classModifiers.Add(SyntaxKind.PartialKeyword.WithSpace());
        }
        else
        {
            classModifiers.AddRange(_metadata.Owner.Modifiers);
            foreach (var member in _metadata.Owner.Members)
            {
                switch (member)
                {
                    case BaseTypeDeclarationSyntax baseTypeDeclarationSyntax:
                        _buildContext.NameService.ReserveName(baseTypeDeclarationSyntax.Identifier.Text);
                        break;

                    case MethodDeclarationSyntax methodDeclarationSyntax:
                        _buildContext.NameService.ReserveName(methodDeclarationSyntax.Identifier.Text);
                        break;

                    case BaseFieldDeclarationSyntax fieldDeclarationSyntax:
                        foreach (var variable in fieldDeclarationSyntax.Declaration.Variables)
                        {
                            _buildContext.NameService.ReserveName(variable.Identifier.Text);
                        }

                        break;
                }
            }
        }

        _bindingsProbe.Probe();
        
        var resolverClass = SyntaxRepo.ClassDeclaration(_metadata.ComposerTypeName)
            .WithKeyword(SyntaxKind.ClassKeyword.WithSpace())
            .AddModifiers(classModifiers.ToArray())
            .AddMembers(
                _membersBuilder
                    .OrderBy(i => i.Order)
                    .SelectMany(i => i.BuildMembers(semanticModel))
                    .Select(i => i.WithNewLine().WithNewLine())
                    .ToArray())
            .WithNewLine()
            .WithPragmaWarningDisable(0067, 8600, 8602, 8603, 8604, 8618, 8625);

        var rootNode = _compilationUnitSyntaxBuilder.CreateRootNode(resolverClass);
        var sampleDependency = _metadata.Bindings.LastOrDefault()?.Dependencies.FirstOrDefault()?.ToString() ?? "T";
        _diagnostic.Information(Diagnostics.Information.Generated, $"{_metadata.ComposerTypeName} was generated. Please use a method like {_metadata.ComposerTypeName}.Resolve<{sampleDependency}>() to create a composition root.", _metadata.Bindings.Where(i => i.Location != default).Select(i => i.Location!).ToArray());
        return rootNode;
    }
}