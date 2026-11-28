using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Hosting;

namespace Build.Core;

sealed class RazorTemplateEngine
{
    // The items are loaded once per engine instance, which the build composition creates only once.
    private readonly IReadOnlyList<RazorCompiledItem> _compiledItems =
        new RazorCompiledItemLoader().LoadItems(typeof(RazorTemplateEngine).Assembly);

    public async Task<string> RenderAsync<TModel>(string template, TModel model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiledItem = _compiledItems.FirstOrDefault(item => item.Identifier == template)
                           ?? throw new InvalidOperationException($"Cannot find Razor template '{template}'.");
        // StringWriter has no IAsyncDisposable, so 'await using' cannot be applied here.
        // ReSharper disable once UseAwaitUsing
        using var writer = new StringWriter();
        var page = (RazorPage<TModel>)Activator.CreateInstance(compiledItem.Type)!;
        page.ViewData = new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };
        page.ViewContext = new ViewContext { Writer = writer };
        page.HtmlEncoder = HtmlEncoder.Default;
        await page.ExecuteAsync();
        return writer.ToString();
    }
}
