namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class BuildContext: IBuildContext
    {
        private readonly Dictionary<MemberKey, MemberDeclarationSyntax> _additionalMembers = new();
        private readonly HashSet<BindingMetadata> _additionalBindings = new();
        private readonly HashSet<StatementSyntax> _finalizationStatements = new();
        private readonly Func<INameService> _nameServiceFactory;
        private readonly Func<ITypeResolver> _typeResolverFactory;
        private ResolverMetadata? _metadata;
        private INameService? _nameService;
        private ITypeResolver? _typeResolver;

        public BuildContext(
            [Tag(Tags.Default)] Func<INameService> nameServiceFactory,
            [Tag(Tags.Default)] Func<ITypeResolver> typeResolverFactory)
        {
            _nameServiceFactory = nameServiceFactory;
            _typeResolverFactory = typeResolverFactory;
        }

        public ResolverMetadata Metadata
        {
            get => _metadata ?? throw new InvalidOperationException("Not initialized.");
            set => _metadata = value;
        }

        public INameService NameService => _nameService ?? throw new InvalidOperationException("Not ready.");

        public ITypeResolver TypeResolver => _typeResolver ?? throw new InvalidOperationException("Not ready.");

        public IEnumerable<BindingMetadata> AdditionalBindings => _additionalBindings;

        public IEnumerable<MemberDeclarationSyntax> AdditionalMembers => _additionalMembers.Values;

        public IEnumerable<StatementSyntax> FinalizationStatements => _finalizationStatements;

        public void Prepare()
        {
            _additionalBindings.Clear();
            _nameService = _nameServiceFactory();
            _typeResolver = _typeResolverFactory();
            _additionalMembers.Clear();
        }

        public void AddBinding(BindingMetadata binding) => _additionalBindings.Add(binding);

        public MemberDeclarationSyntax GetOrAddMember(MemberKey key, Func<MemberDeclarationSyntax> additionalMemberFactory)
        {
            if (_additionalMembers.TryGetValue(key, out var member))
            {
                return member;
            }

            member = additionalMemberFactory();
            _additionalMembers.Add(key, member);
            return member;
        }

        public void AddFinalizationStatement(StatementSyntax finalizationStatement) =>
            _finalizationStatements.Add(finalizationStatement);
    }
}
