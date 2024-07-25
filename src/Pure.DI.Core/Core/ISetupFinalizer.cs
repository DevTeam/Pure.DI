namespace Pure.DI.Core;

internal interface ISetupFinalizer
{
    MdSetup Finalize(MdSetup setup);
}