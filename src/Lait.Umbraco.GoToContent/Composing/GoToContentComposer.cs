using Lait.GoToContent.Middleware;
using Lait.GoToContent.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Lait.GoToContent.Composing;

public sealed class GoToContentComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IBackOfficeAuthDetector, BackOfficeAuthDetector>();
        builder.Services.AddSingleton<ICurrentNodeResolver, CurrentNodeResolver>();
        builder.Services.AddSingleton<IDeepLinkUrlBuilder, V14DeepLinkUrlBuilder>();
        builder.Services.AddSingleton<ISnippetBuilder, SnippetBuilder>();
        builder.Services.AddTransient<GoToContentMiddleware>();

        builder.Services.Configure<UmbracoPipelineOptions>(opts =>
        {
            opts.AddFilter(new UmbracoPipelineFilter(nameof(GoToContentComposer))
            {
                PostPipeline = app => app.UseMiddleware<GoToContentMiddleware>(),
            });
        });
    }
}
