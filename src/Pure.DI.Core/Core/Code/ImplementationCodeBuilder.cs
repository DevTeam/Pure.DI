// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class ImplementationCodeBuilder: ICodeBuilder<DpImplementation>
{
    private readonly CancellationToken _cancellationToken;

    public ImplementationCodeBuilder(CancellationToken cancellationToken) => 
        _cancellationToken = cancellationToken;

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
        
        BuildConstructor(ctx, ctorArgs, requiredFields, requiredProperties);
        
        var visits = new List<(Action Run, int? Ordinal)>();
        foreach (var field in implementation.Fields.Where(i => i.Field.IsRequired != true))
        {
            argsWalker.VisitField(Unit.Shared, field);
            var fieldVariable = argsWalker.GetResult().Single();
            visits.Add((VisitFieldAction, field.Ordinal));
            continue;

            void VisitFieldAction() => BuildField(ctx, field, fieldVariable);
        }
        
        foreach (var property in implementation.Properties.Where(i => !i.Property.IsRequired && i.Property.SetMethod?.IsInitOnly != true))
        {
            argsWalker.VisitProperty(Unit.Shared, property);
            var propertyVariable = argsWalker.GetResult().Single();
            visits.Add((VisitFieldAction, property.Ordinal));
            continue;

            void VisitFieldAction() => BuildProperty(ctx, property, propertyVariable);
        }
        
        foreach (var method in implementation.Methods)
        {
            argsWalker.VisitMethod(Unit.Shared, method);
            var methodArgs = argsWalker.GetResult();
            visits.Add((VisitMethodAction, method.Ordinal));
            continue;

            void VisitMethodAction() => BuildMethod(ctx, method, methodArgs);
        }

        foreach (var visit in visits.OrderBy(i => i.Ordinal ?? int.MaxValue))
        {
            _cancellationToken.ThrowIfCancellationRequested();
            visit.Run();
        }

        ctx.Code.AppendLines(ctx.BuildTools.OnCreated(ctx, variable));
    }
    
    private static void BuildConstructor(
        BuildContext ctx,
        ImmutableArray<Variable> constructorArgs,
        ImmutableArray<(Variable RequiredVariable,DpField RequiredField)>.Builder requiredFields,
        ImmutableArray<(Variable RequiredVariable, DpProperty RequiredProperty)>.Builder requiredProperties)
    {
        var variable = ctx.Variable;
        var required = requiredFields.Select(i => (Variable: i.RequiredVariable, i.RequiredField.Field.Name))
            .Concat(requiredProperties.Select(i => (Variable: i.RequiredVariable, i.RequiredProperty.Property.Name)))
            .ToArray();

        var hasRequired = required.Any();
        var args = string.Join(", ", constructorArgs.Select(i => ctx.BuildTools.OnInjected(ctx, i)));
        var newStatement = variable.InstanceType.IsTupleType ? $"({args})" : $"new {variable.InstanceType}({args})";
        ctx.Code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VarName} = {newStatement}{(hasRequired ? "" : ";")}");
        if (!hasRequired)
        {
            return;
        }

        ctx.Code.AppendLine("{");
        using (ctx.Code.Indent())
        {
            for (var index = 0; index < required.Length; index++)
            {
                var (v, name) = required[index];
                ctx.Code.AppendLine($"{name} = {ctx.BuildTools.OnInjected(ctx, v)}{(index < required.Length - 1 ? "," : "")}");
            }
        }

        ctx.Code.AppendLine("};");
    }
    
    private static void BuildField(BuildContext ctx, DpField field, Variable fieldVariable)
    {
        ctx.Code.AppendLine($"{ctx.Variable.VarName}.{field.Field.Name} = {ctx.BuildTools.OnInjected(ctx, fieldVariable)};");
    }

    private static void BuildProperty(BuildContext ctx, DpProperty property, Variable propertyVariable)
    {
        ctx.Code.AppendLine($"{ctx.Variable.VarName}.{property.Property.Name} = {ctx.BuildTools.OnInjected(ctx, propertyVariable)};");
    }

    private static void BuildMethod(BuildContext ctx, DpMethod method, ImmutableArray<Variable> methodArgs)
    {
        var args = string.Join(", ", methodArgs.Select(i => ctx.BuildTools.OnInjected(ctx, i)));
        ctx.Code.AppendLine($"{ctx.Variable.VarName}.{method.Method.Name}({args});");
    }
}