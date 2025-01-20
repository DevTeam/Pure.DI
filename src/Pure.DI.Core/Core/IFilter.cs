namespace Pure.DI.Core;

internal interface IFilter
{
    bool IsMeet(
        MdSetup setup,
        params (Hint regularExpressionSetting, Hint wildcardSettings, Func<string> textProvider)[] settings);
}