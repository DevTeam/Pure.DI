namespace Pure.DI.Core.Code;

internal interface IFormatter
{
    string Format(Root root);
    
    string FormatRef(Root root);
    
    string FormatRef(string text);
    
    string FormatRef(MdSetup setup, ITypeSymbol type);
}