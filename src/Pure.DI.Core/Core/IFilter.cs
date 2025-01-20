namespace Pure.DI.Core;

internal interface IFilter
{
    bool IsMeetRegularExpressions(MdSetup setup, params (Hint setting, Lazy<string> textProvider)[] settings);
    
    bool IsMeetWildcards(MdSetup setup, params (Hint setting, Lazy<string> textProvider)[] settings);
}