// ReSharper disable InconsistentNaming
namespace Pure.DI.Core.Models;

internal enum Setting
{
    /// <summary>
    /// On or Off.
    /// <c>On</c> by default. 
    /// </summary>
    Resolve,
    
    /// <summary>
    /// On or Off.
    /// <c>On</c> by default. 
    /// </summary>
    OnInstanceCreation,
    
    /// <summary>
    /// On or Off.
    /// <c>Off</c> by default. 
    /// </summary>
    OnDependencyInjection,
    
    /// <summary>
    /// Regular expression.
    /// ".+" by default. 
    /// </summary>
    OnDependencyInjectionImplementationTypeNameRegularExpression,
    
    /// <summary>
    /// Regular expression.
    /// ".+" by default. 
    /// </summary>
    OnDependencyInjectionContractTypeNameRegularExpression,
    
    /// <summary>
    /// Regular expression.
    /// ".+" by default. 
    /// </summary>
    OnDependencyInjectionTagRegularExpression,
    
    /// <summary>
    /// On or Off.
    /// <c>Off</c> by default. 
    /// </summary>
    ToString,
    
    /// <summary>
    /// On or Off.
    /// <c>On</c> by default. 
    /// </summary>
    ThreadSafe
}