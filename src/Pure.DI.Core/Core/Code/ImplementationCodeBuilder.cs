// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable InvertIf
namespace Pure.DI.Core.Code;

using System.Collections;
using System.Text;
using static Lifetime;

sealed class ImplementationCodeBuilder(
    Func<IBuilder<CodeContext, IEnumerator>> variablesCodeBuilderFactory,
    Func<IEnumerable<VarInjection>, IVariablesWalker> varsWalkerFactory,
    IInjections injections,
    IBuildTools buildTools,
    ITypeResolver typeResolver,
    IVariableTools variableTools,
    CancellationToken cancellationToken)
    : IBuilder<CodeBuilderContext, IEnumerator>
{
    public IEnumerator Build(CodeBuilderContext data)
    {
        var (ctx, varInjections) = data;
        var varInjection = ctx.VarInjection;
        var var = varInjection.Var;
        var lines = ctx.Lines;
        var implementation = var.AbstractNode.Node.Implementation!;
        if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var implementationDependencies))
        {
            var injectionsList = new List<VarInjection>(implementationDependencies.Count);
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var dependency in implementationDependencies)
            {
                injectionsList.Add(ctx.VarsMap.GetInjection(ctx.RootContext.Graph, dependency.Injection, dependency.Source));
            }

            injectionsList.Sort(variableTools.InjectionComparer);
            foreach (var dependencyVar in injectionsList)
            {
                yield return variablesCodeBuilderFactory().Build(ctx.CreateChild(dependencyVar));
                varInjections.Add(dependencyVar);
            }
        }

        var varsWalker = varsWalkerFactory(varInjections);
        varsWalker.VisitConstructor(Unit.Shared, implementation.Constructor);
        var ctorArgs = varsWalker.GetResult();

        var requiredFields = ImmutableArray.CreateBuilder<(VarInjection RequiredVarInjection, DpField RequiredField)>();
        foreach (var requiredField in implementation.Fields)
        {
            if (requiredField.Field.IsRequired)
            {
                varsWalker.VisitField(Unit.Shared, requiredField, null);
                var dependencyVar = varsWalker.GetResult().Single();
                requiredFields.Add((dependencyVar, requiredField));
            }
        }

        var requiredProperties = ImmutableArray.CreateBuilder<(VarInjection RequiredVarInjection, DpProperty RequiredProperty)>();
        foreach (var requiredProperty in implementation.Properties)
        {
            if (requiredProperty.Property.IsRequired || requiredProperty.Property.SetMethod?.IsInitOnly == true)
            {
                varsWalker.VisitProperty(Unit.Shared, requiredProperty, null);
                var dependencyVar = varsWalker.GetResult().Single();
                requiredProperties.Add((dependencyVar, requiredProperty));
            }
        }

        var visits = new List<(Action<CodeContext, string> Run, int? Ordinal, int Kind, int Order)>();
        var visitOrder = 0;
        foreach (var field in implementation.Fields)
        {
            if (!field.Field.IsRequired)
            {
                varsWalker.VisitField(Unit.Shared, field, null);
                var dependencyVar = varsWalker.GetResult().Single();
                visits.Add((VisitFieldAction, field.Ordinal, 0, visitOrder++));
                continue;

                void VisitFieldAction(CodeContext context, string name) => injections.FieldInjection(name, context, field, dependencyVar);
            }
        }

        foreach (var property in implementation.Properties)
        {
            if (!property.Property.IsRequired && property.Property.SetMethod?.IsInitOnly != true)
            {
                varsWalker.VisitProperty(Unit.Shared, property, null);
                var dependencyVar = varsWalker.GetResult().Single();
                visits.Add((VisitFieldAction, property.Ordinal, 1, visitOrder++));
                continue;

                void VisitFieldAction(CodeContext context, string name) => injections.PropertyInjection(name, context, property, dependencyVar);
            }
        }

        foreach (var method in implementation.Methods)
        {
            varsWalker.VisitMethod(Unit.Shared, method, null);
            var methodVars = varsWalker.GetResult();
            visits.Add((VisitMethodAction, method.Ordinal, 2, visitOrder++));
            continue;

            void VisitMethodAction(CodeContext context, string name) => injections.MethodInjection(name, context, method, methodVars);
        }

        var onCreatedStatements = buildTools.OnCreated(ctx, varInjection);
        var hasOnCreatedStatements = onCreatedStatements.Count > 0;
        var hasAlternativeInjections = visits.Count > 0;
        var tempVariableInit =
            ctx.RootContext.IsThreadSafeEnabled
            && var.AbstractNode.ActualLifetime is not Transient and not PerBlock
            && (hasAlternativeInjections || hasOnCreatedStatements);

        var tempVar = var;
        if (tempVariableInit)
        {
            tempVar = var with { NameOverride = $"{var.Declaration.Name}{Names.TempInstanceValueNameSuffix}" };
            lines.AppendLine($"{typeResolver.Resolve(ctx.RootContext.Graph.Source, tempVar.InstanceType)} {tempVar.Name};");
            if (onCreatedStatements.Count > 0)
            {
                onCreatedStatements = buildTools.OnCreated(ctx, varInjection with { Var = tempVar });
            }
        }

        var instantiation = CreateInstantiation(ctx, ctorArgs, requiredFields, requiredProperties);
        if (var.AbstractNode.ActualLifetime is not Transient
            || hasAlternativeInjections
            || tempVariableInit
            || hasOnCreatedStatements)
        {
            lines.Append($"{buildTools.GetDeclaration(ctx, var.Declaration, useVar: true)}{tempVar.Name} = ");
            lines.Append(instantiation);
            lines.AppendLine(";");
        }
        else
        {
            var.CodeExpression = instantiation;
        }

        foreach (var visit in visits
                     .OrderBy(i => i.Ordinal ?? int.MaxValue)
                     .ThenBy(i => i.Kind)
                     .ThenBy(i => i.Order))
        {
            cancellationToken.ThrowIfCancellationRequested();
            visit.Run(ctx, tempVar.Name);
        }

        lines.AppendLines(onCreatedStatements);
        if (tempVariableInit)
        {
            lines.AppendLine($"{Names.SystemNamespace}Threading.Thread.MemoryBarrier();");
            lines.AppendLine($"{var.Name} = {tempVar.Name};");
        }
    }

    private string CreateInstantiation(
        CodeContext ctx,
        IReadOnlyCollection<VarInjection> ctorArgs,
        IReadOnlyCollection<(VarInjection RequiredVariable, DpField RequiredField)> requiredFields,
        IReadOnlyCollection<(VarInjection RequiredVariable, DpProperty RequiredProperty)> requiredProperties)
    {
        var var = ctx.VarInjection.Var;
        var code = new StringBuilder();
        var required = new List<(VarInjection Variable, string Name)>(requiredFields.Count + requiredProperties.Count);
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var requiredField in requiredFields.OrderBy(i => i.RequiredField.Ordinal ?? int.MaxValue))
        {
            required.Add((requiredField.RequiredVariable, requiredField.RequiredField.Field.Name));
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var requiredProperty in requiredProperties.OrderBy(i => i.RequiredProperty.Ordinal ?? int.MaxValue))
        {
            required.Add((requiredProperty.RequiredVariable, requiredProperty.RequiredProperty.Property.Name));
        }

        var instanceType = RemoveNullableAnnotation(var.InstanceType);
        if (var.InstanceType.IsTupleType)
        {
            code.Append('(');
        }
        else
        {
            code.Append($"new {typeResolver.Resolve(ctx.RootContext.Graph.Source, instanceType)}(");
        }

        var argumentIndex = 0;
        foreach (var ctorArg in ctorArgs)
        {
            if (argumentIndex++ > 0)
            {
                code.Append(", ");
            }

            code.Append(buildTools.OnInjected(ctx, ctorArg));
        }

        code.Append(')');
        if (required.Count > 0)
        {
            code.Append($" {LinesExtensions.BlockStart} ");
            for (var index = 0; index < required.Count; index++)
            {
                var (v, name) = required[index];
                code.Append($"{name} = {buildTools.OnInjected(ctx, v)}{(index < required.Count - 1 ? ", " : "")}");
            }

            code.Append($" {LinesExtensions.BlockFinish}");
        }

        return code.ToString();
    }

    private static ITypeSymbol RemoveNullableAnnotation(ITypeSymbol type) =>
        type switch
        {
            INamedTypeSymbol namedType => namedType.WithNullableAnnotation(NullableAnnotation.NotAnnotated),
            IArrayTypeSymbol arrayType => arrayType.WithNullableAnnotation(NullableAnnotation.NotAnnotated),
            _ => type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
        };
}
