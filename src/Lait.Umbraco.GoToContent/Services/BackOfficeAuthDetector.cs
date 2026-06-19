using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;

namespace Lait.GoToContent.Services;

internal sealed class BackOfficeAuthDetector : IBackOfficeAuthDetector
{
    // Same-assembly consumers reference this sentinel directly to read the cached
    // bool without re-running AuthenticateAsync (e.g. the body-rewriter middleware).
    internal static readonly object HttpContextItemsKey = new();

    private readonly IOptions<SecuritySettings> _securitySettings;
    private readonly ILogger<BackOfficeAuthDetector> _log;

    public BackOfficeAuthDetector(
        IOptions<SecuritySettings> securitySettings,
        ILogger<BackOfficeAuthDetector> log)
    {
        _securitySettings = securitySettings;
        _log = log;
    }

    public async Task<bool> IsAuthenticatedBackOfficeUserAsync(HttpContext context)
    {
        if (context.Items.TryGetValue(HttpContextItemsKey, out var cached) && cached is bool b)
        {
            return b;
        }

        var cookieName = _securitySettings.Value.AuthCookieName;
        if (!context.Request.Cookies.ContainsKey(cookieName))
        {
            _log.LogTrace("Cookie pre-flight short-circuited — anonymous request, skipping AuthenticateAsync.");
            context.Items[HttpContextItemsKey] = false;
            return false;
        }

        try
        {
            var authResult = await context.AuthenticateAsync(Constants.Security.BackOfficeAuthenticationType);
            var ok = authResult.Succeeded
                     && authResult.Principal?.Identity?.IsAuthenticated == true;

            context.Items[HttpContextItemsKey] = ok;
            return ok;
        }
        catch
        {
            context.Items[HttpContextItemsKey] = false;
            return false;
        }
    }
}
