using Umbraco.Cms.Core.Web;

namespace Lait.GoToContent.Services;

internal sealed class CurrentNodeResolver : ICurrentNodeResolver
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public CurrentNodeResolver(IUmbracoContextAccessor umbracoContextAccessor)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    public Guid? GetCurrentNodeKey()
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var ctx))
        {
            return null;
        }
        return ctx.PublishedRequest?.PublishedContent?.Key;
    }
}
