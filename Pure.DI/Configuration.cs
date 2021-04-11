namespace Pure.DI
{
    using System.Collections.Generic;
    using Core;
    using IoC;
    using Microsoft.CodeAnalysis;
    using static IoC.Lifetime;
    using static Core.Tags;
    using IBuildContext = Core.IBuildContext;

    internal class Configuration: IoC.IConfiguration
    {
        public IEnumerable<IToken> Apply(IMutableContainer container)
        {
            var autowiringStrategy = AutowiringStrategies
                .AspectOriented()
                .Tag<TagAttribute>(i => i.Tag);

            yield return container
                .Bind<IAutowiringStrategy>().To(ctx => autowiringStrategy)
                .Bind<IStdOut>().As(Singleton).To<StdOut>()
                .Bind<IStdErr>().As(Singleton).To<StdErr>()
                .Bind<ITracer>().As(Singleton).To<Tracer>()
                .Bind<IDiagnostic>().As(Singleton).To<DefaultDiagnostic>()
                .Bind<ISourceBuilder>().To<SourceBuilder>()
                .Bind<IBuildContext>().As(Singleton).To<BuildContext>()
                .Bind<INameService>().Tag(Default).To<NameService>()
                .Bind<ITypeResolver>().Tag(Default).To<TypeResolver>()
                .Bind<SemanticModel>().To(ctx => ctx.Container.Inject<IBuildContext>().SemanticModel)
                .Bind<ResolverMetadata>().To(ctx => ctx.Container.Inject<IBuildContext>().Metadata)
                .Bind<INameService>().To(ctx => ctx.Container.Inject<IBuildContext>().NameService)
                .Bind<ITypeResolver>().To(ctx => ctx.Container.Inject<IBuildContext>().TypeResolver)
                .Bind<IConstructorsResolver>().To<ConstructorsResolver>()
                .Bind<IObjectBuilder>().As(Singleton).Tag(AutowiringBuilder).To<AutowiringObjectBuilder>()
                .Bind<IObjectBuilder>().As(Singleton).Tag(FactoryBuilder).To<FactoryObjectBuilder>()
                .Bind<IObjectBuilder>().As(Singleton).Tag(ArrayBuilder).To<ArrayObjectBuilder>()
                .Bind<IFallbackStrategy>().As(Singleton).To<FallbackStrategy>()
                .Bind<IResolverBuilder>().As(Singleton).To<ResolverBuilder>()
                .Bind<IMetadataWalker>().To<MetadataWalker>()
                .Bind<IResolverMethodsBuilder>().To<ResolverMethodsBuilder>()
                .Bind<IBindingsProbe>().As(Singleton).To<BindingsProbe>()
                .Bind<IBindingResultStrategy>().As(Singleton).Tag(AsIsResult).To<AsIsBindingResultStrategy>()
                .Bind<IBindingResultStrategy>().As(Singleton).Tag(GenericResult).To<GenericBindingResultStrategy>()
                .Bind<IResolveMethodBuilder>().Tag(StaticResolve).To<StaticResolveMethodBuilder>()
                .Bind<IResolveMethodBuilder>().Tag(StaticWithTag).To<StaticWithTagResolveMethodBuilder>()
                .Bind<IResolveMethodBuilder>().Tag(GenericStaticResolve).To<GenericStaticResolveMethodBuilder>()
                .Bind<IResolveMethodBuilder>().Tag(GenericStaticWithTag).To<GenericStaticWithTagResolveMethodBuilder>()
                .Bind<IBindingExpressionStrategy>().Tag(SimpleExpressionStrategy).To<BindingExpressionStrategy>(
                    ctx => new BindingExpressionStrategy(
                        ctx.Container.Inject<IBuildContext>(),
                        ctx.Container.Inject<ITracer>(),
                        ctx.Container.Inject<ITypeResolver>(),
                        ctx.Container.Inject<IBindingResultStrategy>(AsIsResult),
                        null))
                .Bind<IBindingExpressionStrategy>().Tag(GenericExpressionStrategy).To<BindingExpressionStrategy>(
                    ctx => new BindingExpressionStrategy(
                        ctx.Container.Inject<IBuildContext>(),
                        ctx.Container.Inject<ITracer>(),
                        ctx.Container.Inject<ITypeResolver>(),
                        ctx.Container.Inject<IBindingResultStrategy>(GenericResult),
                        ctx.Container.Inject<IBindingExpressionStrategy>(SimpleExpressionStrategy)))
                .Bind<IBindingStatementsStrategy>().As(Singleton).Tag(TypeStatementsStrategy).To<TypeBindingStatementsStrategy>()
                .Bind<IBindingStatementsStrategy>().As(Singleton).Tag(TypeAndTagStatementsStrategy).To<TypeAndTagBindingStatementsStrategy>()
                .Bind<ITypesMap>().To<TypesMap>()
                .Bind<IAttributesService>().To<AttributesService>()
                .Bind<ISyntaxContextReceiver>().To<SyntaxContextReceiver>();
        }
    }
}
