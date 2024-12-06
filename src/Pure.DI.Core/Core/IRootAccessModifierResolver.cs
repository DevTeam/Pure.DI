namespace Pure.DI.Core;

internal interface IRootAccessModifierResolver
{
    Accessibility Resolve(Root root);
}