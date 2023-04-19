namespace Pure.DI.Core;

internal interface IFilter
{
    bool IsMeetRegularExpression(MdSetup setup, params (Hint setting, string value)[] settings);
}