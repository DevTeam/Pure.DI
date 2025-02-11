// ReSharper disable MemberCanBePrivate.Global
namespace Pure.DI.Core;

internal sealed class NameFormatter : INameFormatter
{
    private static readonly SymbolDisplayFormat TypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.CollapseTupleTypes | SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix
    );

    private static readonly SymbolDisplayFormat FullTypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.CollapseTupleTypes | SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix
    );

    public string Format(string nameTemplate,
        INamedTypeSymbol? type,
        object? tag)
    {
        if (SyntaxFacts.IsValidIdentifier(nameTemplate))
        {
            return nameTemplate;
        }

        var name = nameTemplate
            .Replace("{type}", type is not null ? type.ToDisplayString(NullableFlowState.NotNull, TypeFormat) : "")
            .Replace("{TYPE}", type is not null ? type.ToDisplayString(NullableFlowState.NotNull, FullTypeFormat) : "")
            .Replace("{tag}", tag is not null ?tag.ToString() : "");

        return ToValidIdentifier(name);
    }

    internal static string ToValidIdentifier(string text)
    {
        if (SyntaxFacts.IsValidIdentifier(text))
        {
            return text;
        }

        var chars = text.ToArray();
        var size = 0;
        for (var i = 0; i < chars.Length; i++)
        {
            ref var ch = ref chars[i];
            if (i == 0)
            {
                if (!SyntaxFacts.IsIdentifierStartCharacter(ch))
                {
                    chars[size++] = '_';
                }
                else
                {
                    chars[size++] = ch;
                }
            }
            else
            {
                if (!SyntaxFacts.IsIdentifierPartCharacter(ch))
                {
                    switch (ch)
                    {
                        case '.':
                        case '`':
                        case ' ':
                        case ':':
                        case ']':
                        case '>':
                            break;

                        default:
                            chars[size++] = '_';
                            break;
                    }
                }
                else
                {
                    chars[size++] = ch;
                }
            }
        }

        return new string(chars, 0, size);
    }
}