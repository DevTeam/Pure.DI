namespace Pure.DI.Core;

interface IFilter
{
    bool IsMeet(
        MdSetup setup,
        params (Hint regularExpressionSetting, Hint wildcardSettings, Func<string> textProvider)[] settings);
}