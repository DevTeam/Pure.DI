// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Build.Tools;

internal class Properties : IProperties
{
    public string this[string name]
    {
        get
        {
            if (Props.TryGetValue(name, out var prop) && !string.IsNullOrWhiteSpace(prop))
            {
                return prop;
            }

            WriteLine($"The property \"{name}\" was not defined, empty value was used.", Color.Details);
            return "";    
        }
    }
}