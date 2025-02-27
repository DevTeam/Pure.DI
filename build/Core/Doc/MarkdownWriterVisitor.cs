// ReSharper disable StringLiteralTypo

namespace Build.Core.Doc;

class MarkdownWriterVisitor(IMarkdownParts markdownParts) : IDocumentVisitor<MarkdownWriterContext>
{
    public async Task<MarkdownWriterContext> StartTypeVisitAsync(MarkdownWriterContext ctx, DocumentType type, CancellationToken cancellationToken)
    {
        if (ctx.Namespace != type.TypeName.NamespaceName && !string.IsNullOrEmpty(ctx.Namespace))
        {
            ctx = ctx with { Namespace = "" };
            await FinishDetails(ctx);
        }

        if (!ctx.Filter(type.TypeName))
        {
            return ctx with { IsSkipping = true };
        }

        ctx = ctx with { IsSkipping = false };
        if (ctx.Namespace != type.TypeName.NamespaceName)
        {
            ctx = ctx with { Namespace = type.TypeName.NamespaceName };
            await StartDetails(ctx, type.TypeName.NamespaceName);
        }

        await StartDetails(ctx, type.TypeName.TypeName);
        return ctx;
    }

    public async Task<MarkdownWriterContext> FinishTypeVisitAsync(MarkdownWriterContext ctx, DocumentType type, CancellationToken cancellationToken)
    {
        if (ctx.IsSkipping)
        {
            return ctx with { IsSkipping = false };
        }

        await FinishDetails(ctx);
        return ctx;
    }

    public async Task<MarkdownWriterContext> StartMemberVisitAsync(MarkdownWriterContext ctx, DocumentMember member, CancellationToken cancellationToken)
    {
        if (ctx.IsSkipping)
        {
            return ctx;
        }

        await StartDetails(ctx, $"{member.Kind} {member.TypeName.MemberName}");
        return ctx with { TrimStart = true };
    }

    public async Task<MarkdownWriterContext> FinishMemberVisitAsync(MarkdownWriterContext ctx, DocumentMember member, CancellationToken cancellationToken)
    {
        if (ctx.IsSkipping)
        {
            return ctx;
        }

        await FinishDetails(ctx);
        return ctx;
    }

    public async Task<MarkdownWriterContext> StartElementVisitAsync(MarkdownWriterContext ctx, DocumentElement element, CancellationToken cancellationToken)
    {
        if (ctx.IsSkipping)
        {
            return ctx;
        }

        switch (element.Source.Name.LocalName.ToLowerInvariant())
        {
            case "code":
                await ctx.Writer.WriteLineAsync();
                await ctx.Writer.WriteLineAsync("```c#");
                await ctx.Writer.WriteLineAsync();
                break;

            case "c":
                await ctx.Writer.WriteAsync(" `");
                break;

            case "param":
                await ctx.Writer.WriteLineAsync();
                var paramName = element.Source.Attribute("name")?.Value ?? "";
                await ctx.Writer.WriteAsync($" - parameter _{paramName}_ - ");
                break;

            case "returns":
                await ctx.Writer.WriteLineAsync();
                await ctx.Writer.WriteAsync(" - returns ");
                break;

            case "see":
                if (element.Source.Attribute("cref") is {} seeCref)
                {
                    await ctx.Writer.WriteAsync($"_{markdownParts.Join(markdownParts.GetParts(seeCref.Value)[^1..])}_");
                }

                if (element.Source.Attribute("href") is {} seeHref)
                {
                    await ctx.Writer.WriteAsync($"[{element.Source.Value}]({seeHref.Value})");
                }

                break;

            case "seealso":
                if (element.Source.Attribute("cref") is {} seeAlsoCref)
                {
                    await ctx.Writer.WriteLineAsync();
                    await ctx.Writer.WriteLineAsync($"See also _{markdownParts.Join(markdownParts.GetParts(seeAlsoCref.Value)[^1..])}_.");
                }

                if (element.Source.Attribute("href") is {} seeAlsoHref)
                {
                    await ctx.Writer.WriteLineAsync();
                    await ctx.Writer.WriteAsync($"See also [{element.Source.Value}]({seeAlsoHref.Value})");
                }

                break;
        }

        return ctx with { TrimStart = true };
    }

    public async Task<MarkdownWriterContext> FinishElementVisitAsync(MarkdownWriterContext ctx, DocumentElement element, CancellationToken cancellationToken)
    {
        if (ctx.IsSkipping)
        {
            return ctx;
        }

        switch (element.Source.Name.LocalName.ToLowerInvariant())
        {
            case "code":
                await ctx.Writer.WriteLineAsync();
                await ctx.Writer.WriteLineAsync("```");
                await ctx.Writer.WriteLineAsync();
                break;

            case "c":
                await ctx.Writer.WriteAsync("` ");
                break;

            case "param":
            case "returns":
                await ctx.Writer.WriteLineAsync();
                break;
        }

        return ctx with { TrimStart = false };
    }

    public async Task<MarkdownWriterContext> TextVisitAsync(MarkdownWriterContext ctx, DocumentText text, CancellationToken cancellationToken)
    {
        if (ctx.IsSkipping)
        {
            return ctx;
        }

        var str = text.Text.Value;
        if (ctx.TrimStart)
        {
            str = str.TrimStart();
        }

        await ctx.Writer.WriteAsync(str);
        return ctx;
    }

    public async Task<MarkdownWriterContext> CompletedAsync(MarkdownWriterContext ctx, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(ctx.Namespace))
        {
            await FinishDetails(ctx);
        }

        return ctx;
    }

    private static async Task StartDetails(MarkdownWriterContext ctx, string title)
    {
        await ctx.Writer.WriteLineAsync("");
        await ctx.Writer.WriteLineAsync($"<details><summary>{title}</summary><blockquote>");
        await ctx.Writer.WriteLineAsync("");
    }

    private static async Task FinishDetails(MarkdownWriterContext ctx)
    {
        await ctx.Writer.WriteLineAsync("");
        await ctx.Writer.WriteLineAsync("</blockquote></details>");
        await ctx.Writer.WriteLineAsync("");
    }
}