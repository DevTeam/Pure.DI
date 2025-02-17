namespace Pure.DI.Core.Code;

interface IFormatter
{
    string Format(Root root);

    string FormatRef(MdSetup setup, Root root);

    string FormatRef(string text);

    string FormatRef(Lifetime lifetime);

    string FormatRef(ITypeSymbol type);
}