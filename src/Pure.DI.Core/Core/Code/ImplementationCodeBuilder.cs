// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf
namespace Pure.DI.Core.Code;

internal class ImplementationCodeBuilder(CancellationToken cancellationToken)
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

            void VisitFieldAction(BuildContext context) => FieldInjection(context, field, fieldVariable);
        }
        
        foreach (var property in implementation.Properties.Where(i => !i.Property.IsRequired && i.Property.SetMethod?.IsInitOnly != true))
        {
            argsWalker.VisitProperty(Unit.Shared, property);
            var propertyVariable = argsWalker.GetResult().Single();
            visits.Add((VisitFieldAction, property.Ordinal));
            continue;

            void VisitFieldAction(BuildContext context) => PropertyInjection(context, property, propertyVariable);
        }
        
        foreach (var method in implementation.Methods)
        {
            argsWalker.VisitMethod(Unit.Shared, method);
            var methodArgs = argsWalker.GetResult();
            visits.Add((VisitMethodAction, method.Ordinal));
            continue;

            void VisitMethodAction(BuildContext context) => MethodInjection(context, method, methodArgs);
        }

        var onCreatedStatements = ctx.BuildTools.OnCreated(ctx, ctx.Variable).ToArray();
        var hasOnCreatedHandler = ctx.BuildTools.OnCreated(ctx, variable).Any();
        var hasAlternativeInjections = visits.Any();
        var tempVariableInit =
            ctx.DependencyGraph.Source.Hints.GetHint(Hint.ThreadSafe, SettingState.On) == SettingState.On
            && ctx.Variable.Node.Lifetime is not Lifetime.Transient and not Lifetime.PerBlock
            && (hasAlternativeInjections || hasOnCreatedHandler);

        if (tempVariableInit)
        {
            ctx = ctx with { Variable = variable with { NameOverride = variable.VariableName + "Temp" } };
            ctx.Code.AppendLine($"{ctx.Variable.InstanceType} {ctx.Variable.VariableName};");
            if (onCreatedStatements.Any())
            {
                onCreatedStatements = ctx.BuildTools.OnCreated(ctx, ctx.Variable).ToArray();
            }
        }

        var instantiation = CreateInstantiation(ctx, ctorArgs, requiredFields, requiredProperties);
        if (variable.Node.Lifetime is not Lifetime.Transient
            || hasAlternativeInjections
            || tempVariableInit
            || hasOnCreatedHandler)
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
    
    private static string CreateInstantiation(
        BuildContext ctx,
        ImmutableArray<Variable> constructorArgs,
        ImmutableArray<(Variable RequiredVariable,DpField RequiredField)>.Builder requiredFields,
        ImmutableArray<(Variable RequiredVariable, DpProperty RequiredProperty)>.Builder requiredProperties)
    {
        var code = new StringBuilder();
        var variable = ctx.Variable;
        var required = requiredFields.Select(i => (Variable: i.RequiredVariable, i.RequiredField.Field.Name))
            .Concat(requiredProperties.Select(i => (Variable: i.RequiredVariable, i.RequiredProperty.Property.Name)))
            .ToArray();

        var args = string.Join(", ", constructorArgs.Select(i => ctx.BuildTools.OnInjected(ctx, i)));
        code.Append(variable.InstanceType.IsTupleType ? $"({args})" : $"new {variable.InstanceType}({args})");
        if (required.Any())
        {
            code.Append(" { ");
            for (var index = 0; index < required.Length; index++)
            {
                var (v, name) = required[index];
                code.Append($"{name} = {ctx.BuildTools.OnInjected(ctx, v)}{(index < required.Length - 1 ? ", " : "")}");
            }
            
            code.Append(" }");
        }
        
        return code.ToString();
    }
    
    private static void FieldInjection(BuildContext ctx, DpField field, Variable fieldVariable)
    {
        ctx.Code.AppendLine($"{ctx.Variable.VariableName}.{field.Field.Name} = {ctx.BuildTools.OnInjected(ctx, fieldVariable)};");
    }

    private static void PropertyInjection(BuildContext ctx, DpProperty property, Variable propertyVariable)
    {
        ctx.Code.AppendLine($"{ctx.Variable.VariableName}.{property.Property.Name} = {ctx.BuildTools.OnInjected(ctx, propertyVariable)};");
    }

    private static void MethodInjection(BuildContext ctx, DpMethod method, ImmutableArray<Variable> methodArgs)
    {
        var args = string.Join(", ", methodArgs.Select(i => ctx.BuildTools.OnInjected(ctx, i)));
        ctx.Code.AppendLine($"{ctx.Variable.VariableName}.{method.Method.Name}({args});");
    }
}