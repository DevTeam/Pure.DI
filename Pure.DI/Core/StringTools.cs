// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class StringTools : IStringTools
{
    public string ConvertToTitle(string input) =>
        input switch
        {
            null => string.Empty,
            "" => string.Empty,
            _ => input[0].ToString().ToUpper() + input.Substring(1)
        };
}