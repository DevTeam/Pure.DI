namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class BuildContext: IBuildContext
    {
        private readonly Dictionary<MemberKey, MemberDeclarationSyntax> _additionalMembers = new();
        private readonly HashSet<BindingMetadata> _additionalBindings = new();
        private readonly Func<INameService> _nameServiceFactory;
        private ResolverMetadata? _metadata;
        private SemanticModel? _semanticModel;
        private INameService? _nameService;

        public BuildContext([Tag(Tags.Default)] Func<INameService> nameServiceFactory) =>
            _nameServiceFactory = nameServiceFactory;

        public ResolverMetadata Metadata
        {
            get => _metadata ?? throw new InvalidOperationException("Not initialized.");
            set => _metadata = value;
        }

        public SemanticModel SemanticModel
        {
            get => _semanticModel ?? throw new InvalidOperationException("Not initialized.");
            set => _semanticModel = value;
        }

        public INameService NameService => _nameService ?? throw new InvalidOperationException("Not ready.");

        public IEnumerable<BindingMetadata> AdditionalBindings => _additionalBindings;

        public IEnumerable<MemberDeclarationSyntax> AdditionalMembers => _additionalMembers.Values;

        public void Prepare()
        {
            _additionalBindings.Clear();
            _nameService = _nameServiceFactory();
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
    }
}
