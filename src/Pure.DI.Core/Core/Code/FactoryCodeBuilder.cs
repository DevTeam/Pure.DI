﻿// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable MergeIntoPattern
namespace Pure.DI.Core.Code;

internal class FactoryCodeBuilder(
    IIdGenerator idGenerator,
    INodeInfo nodeInfo,
    IArguments arguments,
    ITypeResolver typeResolver,
    ICompilations compilations)
    : ICodeBuilder<DpFactory>
{
    public static readonly ParenthesizedLambdaExpressionSyntax DefaultBindAttrParenthesizedLambda = SyntaxFactory.ParenthesizedLambdaExpression();
    public static readonly ParameterSyntax DefaultCtxParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("ctx_1182D127"));
    public const string DefaultInstanceValueName = "instance_1182D127";
    private static readonly string InjectionStatement = $"{Names.InjectionMarker};";

    public void Build(BuildContext ctx, in DpFactory factory)
    {
        var variable = ctx.Variable;
        var code = ctx.Code;
        var level = ctx.Level;
        var lockIsRequired = ctx.LockIsRequired;
        if (nodeInfo.IsLazy(variable.Node))
        {
            level++;
            lockIsRequired = default;
        }
        
        var originalLambda = factory.Source.Factory;
        // Simple factory
        if (originalLambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
        {
            var block = new List<StatementSyntax>();
            foreach (var resolver in factory.Source.Resolvers)
            {
                if (resolver.ArgumentType is not { } argumentType || resolver.Parameter is not {} parameter)
                {
                    continue;
                }
                
                var valueDeclaration = SyntaxFactory.DeclarationExpression(
                    argumentType,
                    SyntaxFactory.SingleVariableDesignation(parameter.Identifier));

                var valueArg =
                    SyntaxFactory.Argument(valueDeclaration)
                        .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword));

                var injection = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(DefaultCtxParameter.Identifier),
                            SyntaxFactory.IdentifierName(nameof(IContext.Inject))))
                    .AddArgumentListArguments(valueArg);

                block.Add(SyntaxFactory.ExpressionStatement(injection));
            }

            if (factory.Source.MemberResolver is {} memberResolver
                && memberResolver.Member is {} member
                && memberResolver.TypeConstructor is {} typeConstructor)
            {
                ExpressionSyntax? value = default;
                var type = memberResolver.ContractType;
                ExpressionSyntax instance = member.IsStatic 
                    ? SyntaxFactory.ParseTypeName(type.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat))
                    : SyntaxFactory.IdentifierName(DefaultInstanceValueName);

                switch (member)
                {
                    case IFieldSymbol fieldSymbol:
                        value = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            instance,
                            SyntaxFactory.IdentifierName(member.Name));
                        break;

                    case IPropertySymbol propertySymbol:
                        value = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            instance,
                            SyntaxFactory.IdentifierName(member.Name));
                        break;

                    case IMethodSymbol methodSymbol:
                        var args = methodSymbol.Parameters
                            .Select(i => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(i.Name)))
                            .ToArray();
                        
                        if (methodSymbol.IsGenericMethod)
                        {
                            var setup = variable.Setup;
                            var binding = variable.Node.Binding;
                            var typeArgs = new List<TypeSyntax>();
                            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                            foreach (var typeArg in methodSymbol.TypeArguments)
                            {
                                var argType = typeConstructor.ConstructReversed(setup, binding.SemanticModel.Compilation, typeArg);
                                if (binding.TypeConstructor is { } bindingTypeConstructor)
                                {
                                    argType = bindingTypeConstructor.Construct(setup, binding.SemanticModel.Compilation, argType);
                                }

                                var typeName = argType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);
                                typeArgs.Add(SyntaxFactory.ParseTypeName(typeName));
                            }
                            
                            value = SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                instance,
                                SyntaxFactory.GenericName(member.Name).AddTypeArgumentListArguments(typeArgs.ToArray()));
                        }
                        else
                        {
                            value = SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                instance,
                                SyntaxFactory.IdentifierName(member.Name));
                        }

                        value = SyntaxFactory
                            .InvocationExpression(value)
                            .AddArgumentListArguments(args);

                        break;
                }

                if (value is not null)
                {
                    block.Add(SyntaxFactory.ReturnStatement(value));
                }
            }
            else
            {
                if (parenthesizedLambda.Block is {} lambdaBlock)
                {
                    block.AddRange(lambdaBlock.Statements);
                }
                else
                {
                    if (parenthesizedLambda.ExpressionBody is { } body)
                    {
                        block.Add(SyntaxFactory.ReturnStatement(body));
                    }
                }
            }

            originalLambda = SyntaxFactory.SimpleLambdaExpression(DefaultCtxParameter)
                .WithBlock(SyntaxFactory.Block(block));
        }

        // Rewrites syntax tree
        var finishLabel = $"{variable.VariableDeclarationName}Finish";
        var injections = new List<FactoryRewriter.Injection>();
        var localVariableRenamingRewriter = new LocalVariableRenamingRewriter(idGenerator, factory.Source.SemanticModel);
        var factoryExpression = localVariableRenamingRewriter.Rewrite(originalLambda);
        var factoryRewriter = new FactoryRewriter(arguments, compilations, factory, variable, finishLabel, injections);
        var lambda = factoryRewriter.Rewrite(factoryExpression);
        new FactoryValidator(factory).Validate(lambda); 
        SyntaxNode syntaxNode = lambda.Block is not null ? lambda.Block : SyntaxFactory.ExpressionStatement((ExpressionSyntax)lambda.Body);
        if (syntaxNode is not BlockSyntax)
        {
            code.Append($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName} = ");
        }
        else
        {
            if (!variable.IsDeclared)
            {
                code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName};");
            }
        }

        // Replaces injection markers by injection code
        if (variable.Args.Count != injections.Count)
        {
            throw new CompileErrorException(
                $"{variable.Node.Lifetime} lifetime does not support cyclic dependencies.",
                factory.Source.Source.GetLocation(),
                LogId.ErrorInvalidMetadata);
        }
        
        using var resolvers = injections
            .Zip(variable.Args, (injection, argument) => (injection, argument))
            .GetEnumerator();

        var injectionsCtx = ctx;
        if (variable.IsLazy && variable.Node.Accumulators.Count > 0)
        {
            injectionsCtx = injectionsCtx with
            {
                Accumulators = injectionsCtx.Accumulators.AddRange(
                    variable.Node.Accumulators
                        .Select(accumulator => accumulator with { IsDeclared = false }))
            };
        }
        
        var text = syntaxNode.GetText();
        foreach (var textLine in text.Lines)
        {
            var line = text.ToString(textLine.Span);
            if (line.Contains(InjectionStatement) && resolvers.MoveNext())
            {
                // When an injection marker
                var (injection, argument) = resolvers.Current;
                using (code.Indent())
                {
                    ctx.StatementBuilder.Build(injectionsCtx with { Level = level, Variable = argument.Current, LockIsRequired = lockIsRequired }, argument);
                    code.AppendLine($"{(injection.DeclarationRequired ? $"{typeResolver.Resolve(ctx.DependencyGraph.Source, argument.Current.Injection.Type)} " : "")}{injection.VariableName} = {ctx.BuildTools.OnInjected(ctx, argument.Current)};");
                }
            }
            else
            {
                // When a code
                var len = 0;
                for (; len < line.Length && line[len] == ' '; len++)
                {
                }

                code.AppendLine(line);
            }
        }

        if (factoryRewriter.IsFinishMarkRequired)
        {
            code.AppendLine($"{finishLabel}:;");
        }
        
        ctx.Code.AppendLines(ctx.BuildTools.OnCreated(ctx, variable));
    }
}