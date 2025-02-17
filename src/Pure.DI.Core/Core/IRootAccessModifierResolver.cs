namespace Pure.DI.Core;

interface IRootAccessModifierResolver
{
    Accessibility Resolve(Root root);
}