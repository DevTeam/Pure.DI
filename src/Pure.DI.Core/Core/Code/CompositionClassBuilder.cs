// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

internal sealed class CompositionClassBuilder(
    [Tag(typeof(UsingDeclarationsBuilder))]
    IBuilder<CompositionCode, CompositionCode> usingDeclarations,
    [Tag(typeof(FieldsBuilder))] IBuilder<CompositionCode, CompositionCode> fields,
    [Tag(typeof(ArgFieldsBuilder))] IBuilder<CompositionCode, CompositionCode> argFields,
    [Tag(typeof(ParameterizedConstructorBuilder))]
    IBuilder<CompositionCode, CompositionCode> parameterizedConstructor,
    [Tag(typeof(DefaultConstructorBuilder))]
    IBuilder<CompositionCode, CompositionCode> defaultConstructor,
    [Tag(typeof(ScopeConstructorBuilder))] IBuilder<CompositionCode, CompositionCode> scopeConstructor,
    [Tag(typeof(RootMethodsBuilder))] IBuilder<CompositionCode, CompositionCode> rootProperties,
    [Tag(typeof(ApiMembersBuilder))] IBuilder<CompositionCode, CompositionCode> apiMembers,
    [Tag(typeof(DisposeMethodBuilder))] IBuilder<CompositionCode, CompositionCode> disposeMethod,
    [Tag(typeof(ResolversFieldsBuilder))] IBuilder<CompositionCode, CompositionCode> resolversFields,
    [Tag(typeof(StaticConstructorBuilder))]
    IBuilder<CompositionCode, CompositionCode> staticConstructor,
    [Tag(typeof(ResolverClassesBuilder))] IBuilder<CompositionCode, CompositionCode> resolversClasses,
    [Tag(typeof(ToStringMethodBuilder))] IBuilder<CompositionCode, CompositionCode> toString,
    [Tag(typeof(ClassCommenter))] ICommenter<Unit> classCommenter,
    IInformation information,
    CancellationToken cancellationToken)
    : IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<CompositionCode, CompositionCode>[] _codeBuilders =
    [
        fields,
        argFields,
        parameterizedConstructor,
        defaultConstructor,
        scopeConstructor,
        rootProperties,
        apiMembers,
        disposeMethod,
        resolversFields,
        staticConstructor,
        resolversClasses,
        toString
    ];

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        code.AppendLine("// <auto-generated/>");
        code.AppendLine($"// by {information.Description}");
        if (composition.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable)
        {
            code.AppendLine("#nullable enable annotations");
        }

        code.AppendLine();
        composition = usingDeclarations.Build(composition);

        var nsIndent = Disposables.Empty;
        var name = composition.Source.Source.Name;
        if (!string.IsNullOrWhiteSpace(name.Namespace))
        {
            code.AppendLine($"namespace {name.Namespace}");
            code.AppendLine("{");
            nsIndent = code.Indent();
        }

        classCommenter.AddComments(composition, Unit.Shared);
        code.AppendLine("#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1");
        code.AppendLine($"[{Names.SystemNamespace}Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        code.AppendLine("#endif");
        var implementingInterfaces = new List<string>();
        if (composition.TotalDisposablesCount > 0)
        {
            implementingInterfaces.Add(Names.IDisposableTypeName);
        }

        if (composition.AsyncDisposableCount > 0)
        {
            implementingInterfaces.Add(Names.IAsyncDisposableTypeName);
        }

        code.AppendLine($"partial class {name.ClassName}{(implementingInterfaces.Count > 0 ? ": " + string.Join(", ", implementingInterfaces) : "")}");
        code.AppendLine("{");

        using (code.Indent())
        {
            var prevCount = composition.MembersCount;
            // Generate class members
            foreach (var builder in _codeBuilders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (prevCount != composition.MembersCount)
                {
                    code.AppendLine();
                    prevCount = composition.MembersCount;
                }

                composition = builder.Build(composition);
            }
        }

        code.AppendLine("}");

        // ReSharper disable once InvertIf
        if (!string.IsNullOrWhiteSpace(name.Namespace))
        {
            // ReSharper disable once RedundantAssignment
            nsIndent.Dispose();
            code.AppendLine("}");
        }

        return composition;
    }
}