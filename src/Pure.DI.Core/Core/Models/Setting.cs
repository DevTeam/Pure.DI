// ReSharper disable InconsistentNaming
namespace Pure.DI.Core.Models;

internal enum Setting
{
    /// <summary>
    /// <c>On</c> by default. 
    /// </summary>
    Resolve,
    
    /// <summary>
    /// <c>On</c> by default. 
    /// </summary>
    OnInstanceCreation,
    
    /// <summary>
    /// <c>Off</c> by default. 
    /// </summary>
    OnDependencyInjection,
    
    /// <summary>
    /// <c>Off</c> by default. 
    /// </summary>
    ToString
}