// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class ConstructCodeBuilder(ITypeResolver typeResolver)
    : ICodeBuilder<DpConstruct>
{
    public void Build(BuildContext ctx, in DpConstruct construct)
    {
        switch (construct.Source.Kind)
        {
            case MdConstructKind.Enumerable:
                BuildEnumerable(ctx, construct);
                break;

            case MdConstructKind.Array:
                BuildArray(ctx, construct);
                break;

            case MdConstructKind.Span:
                BuildSpan(ctx, construct);
                break;

            case MdConstructKind.Composition:
                BuildComposition(ctx);
                break;

            case MdConstructKind.OnCannotResolve:
                BuildOnCannotResolve(ctx);
                break;
            
            case MdConstructKind.ExplicitDefaultValue:
                BuildExplicitDefaultValue(ctx, construct);
                break;
            
            case MdConstructKind.AsyncEnumerable:
                BuildEnumerable(ctx, construct,"async ");
                break;
            
            case MdConstructKind.Accumulator:
                break;

            case MdConstructKind.None:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void BuildEnumerable(BuildContext ctx, in DpConstruct enumerable, string methodPrefix = "")
    {
        var variable = ctx.Variable;
        var code = ctx.Code;
        var level = ctx.Level + 1;
        var localFuncName = $"{Names.LocalMethodPrefix}{variable.VariableName}";
        if (enumerable.Source.SemanticModel.Compilation.GetLanguageVersion() >= LanguageVersion.CSharp9)
        {
            code.AppendLine($"[{Names.MethodImplAttribute}(({Names.MethodImplOptions})0x200)]");
        }

        code.AppendLine($"{methodPrefix}{typeResolver.Resolve(variable.InstanceType)} {localFuncName}()");
        code.AppendLine("{");
        using (code.Indent())
        {
            var hasYieldReturn = false;
            foreach (var statement in variable.Args)
            {
                ctx.StatementBuilder.Build(ctx with { Level = level, Variable = statement.Current, LockIsRequired = default }, statement);
                code.AppendLine($"yield return {ctx.BuildTools.OnInjected(ctx, statement.Current)};");
                hasYieldReturn = true;
            }
            
            if (!hasYieldReturn)
            {
                code.AppendLine("yield break;");
            }
        }

        code.AppendLine("}");
        code.AppendLine();
        ctx.Code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName} = {localFuncName}();");
        ctx.Code.AppendLines(ctx.BuildTools.OnCreated(ctx, variable));
    }
    
    private void BuildArray(BuildContext ctx, in DpConstruct array)
    {
        var variable = ctx.Variable;
        var instantiation = $"new {typeResolver.Resolve(array.Source.ElementType)}[{variable.Args.Count.ToString()}] {{ {string.Join(", ", variable.Args.Select(i => ctx.BuildTools.OnInjected(ctx, i.Current)))} }}";
        var onCreated = ctx.BuildTools.OnCreated(ctx, variable).ToArray();
        if (onCreated.Any())
        {
            ctx.Code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName} = {instantiation};");
            ctx.Code.AppendLines(onCreated);
        }
        else
        {
            variable.VariableCode = instantiation;
        }
    }

    private void BuildSpan(BuildContext ctx, in DpConstruct span)
    {
        var variable = ctx.Variable;
        var createArray = $"{typeResolver.Resolve(span.Source.ElementType)}[{variable.Args.Count.ToString()}] {{ {string.Join(", ", variable.Args.Select(i => ctx.BuildTools.OnInjected(ctx, i.Current)))} }}";

        var isStackalloc = 
            span.Source.ElementType.IsValueType
            && span.Binding.SemanticModel.Compilation.GetLanguageVersion() >= LanguageVersion.CSharp7_3;
        
        var createInstance = isStackalloc ? $"stackalloc {createArray}" : $"new {Names.SystemNamespace}Span<{typeResolver.Resolve(span.Source.ElementType)}>(new {createArray})";
        ctx.Code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName} = {createInstance};");
        ctx.Code.AppendLines(ctx.BuildTools.OnCreated(ctx, variable));
    }

    private static void BuildComposition(BuildContext ctx)
    {
        var variable = ctx.Variable;
        ctx.Code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName} = this;");
    }

    private static void BuildOnCannotResolve(BuildContext ctx)
    {
        var variable = ctx.Variable;
        ctx.Code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName} = {Names.OnCannotResolve}<{variable.ContractType}>({variable.Injection.Tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()});");
    }
    
    private static void BuildExplicitDefaultValue(BuildContext ctx, in DpConstruct explicitDefault)
    {
        var variable = ctx.Variable;
        ctx.Code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName} = {explicitDefault.Source.ExplicitDefaultValue.ValueToString()};");
    }
}