// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class DependsOnInstanceMemberValidator(
    ILogger logger,
    ILocationProvider locationProvider,
    ICache<MdSetup, HashSet<string>> instanceMemberNamesCache)
    : IValidator<MdSetup>
{
    public bool Validate(MdSetup setup)
    {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var binding in setup.Bindings)
        {
            if (binding.SourceSetup.Name.Equals(setup.Name))
            {
                continue;
            }

            if (setup.SetupContextMembers.Any(i => i.SetupName.Equals(binding.SourceSetup.Name))
                || setup.DependsOn
                    .SelectMany(dependsOn => dependsOn.Items)
                    .Any(item => item.ContextArgKind == SetupContextKind.Members
                                 && item.CompositionTypeName.Equals(binding.SourceSetup.Name)))
            {
                continue;
            }

            if (binding.Factory is not {} factory)
            {
                continue;
            }

            var setupType = GetContainingType(binding.SourceSetup);
            if (setupType is null)
            {
                continue;
            }

            var instanceMemberNames = instanceMemberNamesCache.Get(binding.SourceSetup, CollectInstanceMemberNames);
            var walker = new InstanceMemberAccessWalker(factory.SemanticModel, setupType, instanceMemberNames);
            walker.Visit(factory.Factory);

            foreach (var access in walker.Accesses)
            {
                logger.CompileWarning(
                    LogMessage.Format(
                        nameof(Strings.Warning_Template_InstanceMemberInDependsOnSetup),
                        Strings.Warning_Template_InstanceMemberInDependsOnSetup,
                        access.MemberName,
                        binding.SourceSetup.Name),
                    ImmutableArray.Create(locationProvider.GetLocation(access.Node)),
                    LogId.WarningInstanceMemberInDependsOnSetup);
            }
        }

        return true;
    }

    private static HashSet<string> CollectInstanceMemberNames(MdSetup setup)
    {
        var names = new HashSet<string>();

        // Use semantic model to collect all non-static member names from the containing type
        // This works for partial classes because semantic model includes all partial parts
        var setupType = GetContainingType(setup);
        if (setupType is null)
        {
            return names;
        }

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var member in setupType.GetMembers())
        {
            if (member.IsStatic)
            {
                continue;
            }

            switch (member)
            {
                case IFieldSymbol field:
                    names.Add(field.Name);
                    break;

                case IPropertySymbol property:
                    names.Add(property.Name);
                    break;

                case IEventSymbol @event:
                    names.Add(@event.Name);
                    break;

                case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
                    names.Add(method.Name);
                    break;
            }
        }

        return names;
    }

    private static INamedTypeSymbol? GetContainingType(MdSetup setup) =>
        setup.Source.Ancestors()
            .OfType<BaseTypeDeclarationSyntax>()
            .FirstOrDefault() is {} typeDeclaration
            ? setup.SemanticModel.GetDeclaredSymbol(typeDeclaration)
            : null;

    private sealed class InstanceMemberAccessWalker(
        SemanticModel semanticModel,
        INamedTypeSymbol setupType,
        HashSet<string> instanceMemberNames)
        : CSharpSyntaxWalker
    {
        private readonly HashSet<TextSpan> _spans = [];
        public List<MemberAccess> Accesses { get; } = [];

        public override void VisitThisExpression(ThisExpressionSyntax node)
        {
            if (node.Parent is MemberAccessExpressionSyntax)
            {
                return;
            }

            Add(node, "this");
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (node.SyntaxTree != semanticModel.SyntaxTree)
            {
                base.VisitIdentifierName(node);
                return;
            }

            // Fast path: check if the identifier name matches any instance member using syntax only
            if (!instanceMemberNames.Contains(node.Identifier.ValueText))
            {
                base.VisitIdentifierName(node);
                return;
            }

            // Only use semantic model for identifiers that could potentially be instance members
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            switch (symbol)
            {
                case IFieldSymbol field when IsInstanceMember(field, setupType):
                    Add(node, field.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                    break;

                case IPropertySymbol property when IsInstanceMember(property, setupType):
                    Add(node, property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                    break;

                case IEventSymbol @event when IsInstanceMember(@event, setupType):
                    Add(node, @event.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                    break;

                case IMethodSymbol { MethodKind: MethodKind.Ordinary } method
                    when IsInstanceMember(method, setupType):
                    Add(node, method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                    break;
            }

            base.VisitIdentifierName(node);
        }

        private void Add(SyntaxNode node, string memberName)
        {
            if (!_spans.Add(node.Span))
            {
                return;
            }

            Accesses.Add(new MemberAccess(node, memberName));
        }

        private static bool IsInstanceMember(ISymbol symbol, INamedTypeSymbol setupType) =>
            !symbol.IsStatic
            && SymbolEqualityComparer.Default.Equals(symbol.ContainingType, setupType);
    }

    private sealed record MemberAccess(SyntaxNode Node, string MemberName);
}