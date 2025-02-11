// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf

namespace Pure.DI.Core.Code;

internal sealed class ImplementationCodeBuilder(
    ITypeResolver typeResolver,
    IInjections injections,
    CancellationToken cancellationToken)
    : ICodeBuilder<DpImplementation>
{
    public void Build(BuildContext ctx, in DpImplementation implementation)
    {
        var variable = ctx.Variable;
        var argsWalker = new DependenciesToVariablesWalker(variable.Args.Select(i => i.Current).ToList());
        argsWalker.VisitConstructor(Unit.Shared, implementation.Constructor);
        var ctorArgs = argsWalker.GetResult();
        var requiredFields = ImmutableArray.CreateBuilder<(Variable RequiredVariable, DpField RequiredField)>();
        foreach (var requiredField in implementation.Fields.Where(i => i.Field.IsRequired).OrderBy(i => i.Ordinal ?? int.MaxValue - 1))
        {
            argsWalker.VisitField(Unit.Shared, requiredField);
            requiredFields.Add((argsWalker.GetResult().Single(), requiredField));
        }

        var requiredProperties = ImmutableArray.CreateBuilder<(Variable RequiredVariable, DpProperty RequiredProperty)>();
        foreach (var requiredProperty in implementation.Properties.Where(i => i.Property.IsRequired || i.Property.SetMethod?.IsInitOnly == true).OrderBy(i => i.Ordinal ?? int.MaxValue))
        {
            argsWalker.VisitProperty(Unit.Shared, requiredProperty);
            requiredProperties.Add((argsWalker.GetResult().Single(), requiredProperty));
        }

        var visits = new List<(Action<BuildContext> Run, int? Ordinal)>();
        foreach (var field in implementation.Fields.Where(i => i.Field.IsRequired != true))
        {
            argsWalker.VisitField(Unit.Shared, field);
            var fieldVariable = argsWalker.GetResult().Single();
            visits.Add((VisitFieldAction, field.Ordinal));
            continue;

            void VisitFieldAction(BuildContext context) => injections.FieldInjection(variable.VariableName, context, field, fieldVariable);
        }

        foreach (var property in implementation.Properties.Where(i => !i.Property.IsRequired && i.Property.SetMethod?.IsInitOnly != true))
        {
            argsWalker.VisitProperty(Unit.Shared, property);
            var propertyVariable = argsWalker.GetResult().Single();
            visits.Add((VisitFieldAction, property.Ordinal));
            continue;

            void VisitFieldAction(BuildContext context) => injections.PropertyInjection(variable.VariableName, context, property, propertyVariable);
        }

        foreach (var method in implementation.Methods)
        {
            argsWalker.VisitMethod(Unit.Shared, method);
            var methodArgs = argsWalker.GetResult();
            visits.Add((VisitMethodAction, method.Ordinal));
            continue;

            void VisitMethodAction(BuildContext context) => injections.MethodInjection(variable.VariableName, context, method, methodArgs);
        }

        var onCreatedStatements = ctx.BuildTools.OnCreated(ctx, ctx.Variable).ToList();
        var hasOnCreatedStatements = ctx.BuildTools.OnCreated(ctx, variable).Any();
        var hasAlternativeInjections = visits.Count > 0;
        var tempVariableInit =
            ctx.DependencyGraph.Source.Hints.IsThreadSafeEnabled
            && ctx.Variable.Node.Lifetime is not Lifetime.Transient and not Lifetime.PerBlock
            && (hasAlternativeInjections || hasOnCreatedStatements);

        if (tempVariableInit)
        {
            ctx = ctx with { Variable = variable with { NameOverride = variable.VariableDeclarationName + "Temp" } };
            ctx.Code.AppendLine($"{typeResolver.Resolve(ctx.Variable.Setup, ctx.Variable.InstanceType)} {ctx.Variable.VariableDeclarationName};");
            if (onCreatedStatements.Count > 0)
            {
                onCreatedStatements = ctx.BuildTools.OnCreated(ctx, ctx.Variable).ToList();
            }
        }

        var instantiation = CreateInstantiation(ctx, ctorArgs, requiredFields, requiredProperties);
        if (variable.Node.Lifetime is not Lifetime.Transient
            || hasAlternativeInjections
            || tempVariableInit
            || hasOnCreatedStatements)
        {
            ctx.Code.Append($"{ctx.BuildTools.GetDeclaration(variable)}{ctx.Variable.VariableName} = ");
            ctx.Code.Append(instantiation);
            ctx.Code.AppendLine(";");
        }
        else
        {
            variable.VariableCode = instantiation;
        }

        foreach (var visit in visits.OrderBy(i => i.Ordinal ?? int.MaxValue))
        {
            cancellationToken.ThrowIfCancellationRequested();
            visit.Run(ctx);
        }

        ctx.Code.AppendLines(onCreatedStatements);
        if (tempVariableInit)
        {
            ctx.Code.AppendLine($"{Names.SystemNamespace}Threading.Thread.MemoryBarrier();");
            ctx.Code.AppendLine($"{variable.VariableName} = {ctx.Variable.VariableName};");
        }
    }

    private string CreateInstantiation(
        BuildContext ctx,
        IReadOnlyCollection<Variable> constructorArgs,
        IReadOnlyCollection<(Variable RequiredVariable, DpField RequiredField)> requiredFields,
        IReadOnlyCollection<(Variable RequiredVariable, DpProperty RequiredProperty)> requiredProperties)
    {
        var code = new StringBuilder();
        var variable = ctx.Variable;
        var required = requiredFields.Select(i => (Variable: i.RequiredVariable, i.RequiredField.Field.Name))
            .Concat(requiredProperties.Select(i => (Variable: i.RequiredVariable, i.RequiredProperty.Property.Name)))
            .ToList();

        var args = string.Join(", ", constructorArgs.Select(i => ctx.BuildTools.OnInjected(ctx, i)));
        code.Append(variable.InstanceType.IsTupleType ? $"({args})" : $"new {typeResolver.Resolve(variable.Setup, variable.InstanceType)}({args})");
        if (required.Count > 0)
        {
            code.Append(" { ");
            for (var index = 0; index < required.Count; index++)
            {
                var (v, name) = required[index];
                code.Append($"{name} = {ctx.BuildTools.OnInjected(ctx, v)}{(index < required.Count - 1 ? ", " : "")}");
            }

            code.Append(" }");
        }

        return code.ToString();
    }
}