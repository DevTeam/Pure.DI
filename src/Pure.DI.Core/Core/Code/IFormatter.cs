namespace Pure.DI.Core.Code;

internal interface IFormatter
{
    string Format(Root root);

    string FormatRef(MdSetup setup, Root root);

    string FormatRef(string text);

    string FormatRef(ITypeSymbol type);
}