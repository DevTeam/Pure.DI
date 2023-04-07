namespace Pure.DI.Core;

internal interface IFilter
{
    bool IsMeetRegularExpression(MdSetup setup, params (Setting setting, string value)[] settings);
}